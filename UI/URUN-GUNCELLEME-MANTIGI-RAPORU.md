# Admin Paneli Ürün Güncelleme Mantığı Raporu

**Uygulama:** `UI/admin`  
**Rapor tarihi:** 3 Ağustos 2026  
**Kapsam:** Ürün düzenleme formu, değişiklik tespiti, varyant güncellemeleri, API çağrı sırası, kısmi kayıt ve eşzamanlılık çakışması davranışı

## 1. Yönetici özeti

Ürün düzenleme ekranı, formdaki bütün ürün ve varyantları her kaydetmede tekrar API'ye göndermek yerine yalnızca kullanıcının gerçekten değiştirdiği bölümleri güncelleyecek şekilde düzenlendi.

Bu değişikliğin temel amacı, özellikle yalnız ana SKU gibi bir ürün alanı düzenlendiğinde mevcut varyantlara gereksiz `PUT` istekleri gönderilmesini önlemektir. Gereksiz varyant güncellemeleri hem fazladan API yükü oluşturuyor hem de backend tarafındaki optimistic concurrency kontrolleri nedeniyle aşağıdaki türde kısmi kayıt hatalarına yol açabiliyordu:

> The record was changed by another operation. Refresh the data and try again.

Yeni akışta:

1. Temel ürün alanları ve varyant alanları ayrı değişiklik niyetleri olarak takip edilir.
2. Dokunulmamış kayıtlı varyantlar doğrulanır fakat mutation listesine alınmaz.
3. Yeni varyantlar yalnızca anlamlı bir satış detayı girilmişse gönderilir.
4. Ürün durumu, öne çıkarma tercihi ve varyant modu yalnızca eski değerinden farklıysa güncellenir.
5. API işlemleri sıralı çalıştırılır ve başarıyla tamamlanan aşamalar kaydedilir.
6. `409 Conflict` durumunda aynı veriyi körlemesine tekrar yazma denenmez.
7. Sunucudaki güncel ürün yeniden okunur ve kullanıcıya güncel kaydı yükleme seçeneği sunulur.

Backend veya API dokümanlarında bu çalışma kapsamında değişiklik yapılmamıştır.

## 2. İlgili frontend dosyaları

| Dosya | Sorumluluk |
| --- | --- |
| `admin/src/modules/products/components/product-form.tsx` | Temel ürün alanlarındaki değişiklikleri takip eder ve Server Action durumunu gösterir. |
| `admin/src/modules/products/components/variant-editor.tsx` | Tekli ve çoklu varyant alanlarını yönetir; değişen kayıtlı varyantları işaretler. |
| `admin/src/modules/products/form-data.ts` | `FormData` verisini doğrular, API DTO'larına dönüştürür ve mutation yapılmayacak varyantları ayıklar. |
| `admin/src/modules/products/actions.ts` | Yetki kontrolünü, API çağrı sırasını, kısmi başarıyı ve `409` davranışını yönetir. |
| `admin/src/modules/products/api.ts` | Ürün, durum, varyant ve görsel endpoint çağrılarını server-only sınırında tutar. |
| `admin/src/modules/products/types.ts` | Form action durumunu ve güncel kaydı yükleme bağlantısını tanımlar. |
| `admin/src/modules/products/form-data.test.ts` | Değişiklik filtreleme ve ana SKU regresyon senaryolarını doğrular. |

## 3. Güncellemenin yüksek seviyeli akışı

Ürün düzenleme isteği aşağıdaki sırayla ilerler:

1. Kullanıcı formdaki alanları değiştirir.
2. Form, değişikliğin temel ürüne mi yoksa belirli bir varyanta mı ait olduğunu işaretler.
3. Form gönderildiğinde Admin oturumu server-side doğrulanır.
4. Bütün form verisi sunucu sınırında parse edilir ve doğrulanır.
5. Yalnız değişen bölümlerden bir mutation listesi oluşur.
6. API çağrıları birbirini bekleyecek şekilde sıralı çalıştırılır.
7. Bütün işlemler başarılıysa ürün detay route'una `?saved=1` ile yönlendirilir.
8. Bir işlem başarısızsa tamamlanan ve başarısız olan aşamalar ayrı ayrı kullanıcıya bildirilir.

Formda hiçbir alan değişmemişse herhangi bir ürün veya varyant mutation çağrısı yapılmaz. Form yine doğrulanır ve başarılı kayıt yönlendirmesi gerçekleştirilir.

## 4. Temel ürün değişikliklerinin takibi

`product-form.tsx`, temel ürün endpoint'ine ait alanları `baseProductFieldNames` kümesiyle ayırır.

Takip edilen temel alanlar:

- Ürün başlığı
- Ana SKU
- Ürün tipi
- URL
- Marka
- Açıklama
- Görüntüleme sırası
- SEO başlığı
- SEO açıklaması
- Vergi oranı
- Tag listesi

Bu alanlardan biri değiştiğinde form içine aşağıdaki niyet alanı eklenir:

```text
baseChanged=on
```

Tag editörü kendi kontrollü yapısını kullandığı için tag ekleme veya kaldırma işlemi ayrıca `baseChanged` durumunu etkinleştirir.

Şu alanlar temel ürün değişiklik listesinden özellikle ayrılmıştır:

| Alan | Güncelleme yöntemi |
| --- | --- |
| Durum | Ayrı `PATCH /status` çağrısı |
| Öne çıkarma | Ayrı `PATCH /featured` çağrısı |
| Varyantlı ürün modu | Ayrı `PATCH /has-variants` çağrısı |
| Varyant satış bilgileri | Varyant kimliği üzerinden ayrı `PUT` çağrısı |

Bu ayrım sayesinde örneğin yalnız durum değiştirildiğinde temel ürün `PUT` isteği gönderilmez.

## 5. Varyant değişikliklerinin takibi

Frontend içindeki her varyant taslağı API'ye gönderilmeyen bir `changed` alanı taşır. Kayıtlı varyantlar ekran ilk açıldığında `changed: false` olarak hazırlanır.

### 5.1 Bir varyant ne zaman değişmiş sayılır?

| Kullanıcı işlemi | İşaretlenen varyantlar |
| --- | --- |
| SKU, barkod, fiyat, karşılaştırma fiyatı, stok veya materyal değişikliği | Yalnız düzenlenen varyant |
| Varyant seçeneği adının değişmesi; örneğin `Renk` → `Kaplama` | Bu seçenek yapısındaki bütün kayıtlı varyantlar |
| Bir seçenek değerinin değişmesi; örneğin `Siyah` → `Mat Siyah` | Bu değeri kullanan kayıtlı çapraz varyantlar |
| Kombinasyon yapısının değişmesi ve kayıtlı varyant kimliğinin başka kombinasyona taşınması | Taşınan kayıtlı varyant |
| Varyantsız ürünün SKU'sunun değişmesi | Tek varsayılan varyant; çünkü ürün SKU'su ve varyant SKU'su ortaktır |
| Çoklu varyantlı ürünün yalnız ana SKU'sunun değişmesi | Hiçbir varyant; yalnız temel ürün değişir |

Değişen kayıtlı varyant için forma aşağıdaki gizli niyet alanı eklenir:

```text
variants.{index}.changed=on
```

Bu alan yalnızca frontend kararında kullanılır. `form-data.ts`, API gövdesini üretmeden önce `changed` alanını payload'dan çıkarır.

### 5.2 Yeni varyant davranışı

Kimliği olmayan satırlar yeni varyant kabul edilir. Yeni bir çapraz kombinasyon otomatik oluşmuş olsa bile aşağıdaki satış detaylarının tamamı boşsa backend'e gönderilmez:

- SKU
- Fiyat
- Karşılaştırma fiyatı
- Barkod
- Materyal
- Açılış maliyeti alanları
- Stok düzeltme nedeni
- Pozitif stok

Kullanıcı bu alanlardan herhangi birini doldurursa satır artık anlamlı kabul edilir; zorunlu SKU ve fiyat kuralları uygulanır. Böylece yarım doldurulmuş varyant sessizce atlanmaz, alan hatası olarak kullanıcıya gösterilir.

Çapraz varyant detaylarında yeni ve tamamen boş bir satır için HTML `required` doğrulaması etkinleştirilmez. Satıra satış bilgisi girildiği anda SKU, fiyat ve stok zorunluluğu devreye girer. Böylece tarayıcı boş satır nedeniyle form gönderimini durdurmaz ve sunucu parser'ı bu satırı güvenle payload dışında bırakabilir.

## 6. Form parse ve doğrulama mantığı

`parseProductForm(formData, "edit")` iki ayrı veri kümesi oluşturur:

1. `parsedVariants`: Formdaki kayıtlı ve anlamlı yeni varyantların tamamı.
2. `variants`: Yalnız API mutation'ı yapılacak varyantlar.

Önemli ayrım şudur: dokunulmamış kayıtlı varyantlar mutation listesinden çıkarılsa da form doğrulamasından tamamen kaçmaz. Birleşik seçenek yapısı ve tekrar eden kombinasyon kontrolleri bütün `parsedVariants` listesi üzerinden yapılır.

Düzenleme modundaki filtreleme kuralı kavramsal olarak şöyledir:

```text
Yeni varyantsa gönder.
Kayıtlı ve changed=true ise gönder.
Kayıtlı ve changed=false ise gönderme.
```

Parse edilen temel güncelleme DTO'su şu alanlarla sınırlıdır:

- `title`
- `mainSku`
- `type`
- `url`
- `brandId`
- `description`
- `displayOrder`
- `seoTitle`
- `seoDescription`
- `tags`
- `taxRateId`

`status`, `isFeatured`, `hasVariants`, koleksiyonlar ve varyantlar temel ürün `PUT` gövdesine eklenmez. Bunlar backend sözleşmesine göre ayrı yönetilir.

## 7. Server Action ve API çağrı sırası

`updateProductAction` bütün mutation işlemlerinden önce `requireAdminActionSession()` çağırır. Aktif Admin oturumu doğrulanamazsa ürün API'sine hiçbir yazma isteği gönderilmez.

Doğrulama başarılı olduğunda çağrı sırası şöyledir:

| Sıra | Koşul | HTTP işlemi | Amaç |
| ---: | --- | --- | --- |
| 1 | `baseChanged=true` | `PUT /api/products/{productId}` | Temel ürün bilgilerini ve tagları güncellemek |
| 2 | Durum eski değerden farklı | `PATCH /api/products/{productId}/status` | Yalnız ürün durumunu güncellemek |
| 3 | Öne çıkarma tercihi farklı | `PATCH /api/products/{productId}/featured` | Yalnız öne çıkarma tercihini güncellemek |
| 4 | Varyant modu farklı | `PATCH /api/products/{productId}/has-variants` | Varyantlı ürün bayrağını güncellemek |
| 5 | Değişen kayıtlı varyant var | `PUT /api/product-variants/{variantId}` | Yalnız değişen varyantı güncellemek |
| 6 | Anlamlı yeni varyant var | `POST /api/product-variants/by-product/{productId}` | Yeni varyantı ürüne eklemek |
| 7 | URL tabanlı görsel DTO'su var | `POST /api/product-images/by-product/{productId}` | Yeni ürün görseli eklemek |

İstekler paralel çalıştırılmaz. Her işlem `await` ile tamamlandıktan sonra sıradaki işleme geçilir. Böylece hangi aşamanın başarılı olduğu kesin olarak izlenebilir ve aynı ürün üzerinde kontrolsüz paralel mutation üretilmez.

Her başarılı çağrıdan sonra işlem adı `completedOperations` listesine eklenir. Bir hata oluşursa döngü ve sonraki işlemler durur.

## 8. Ana SKU güncelleme regresyonunun çözümü

P00004 örneğinde yalnız ana SKU değiştirilmesine rağmen önceki frontend davranışı şu şekildeydi:

1. Temel ürün `PUT` isteği gönderiliyordu.
2. Formdaki dört kayıtlı varyant değişmemiş olsa da dört ayrı varyant `PUT` isteği daha gönderiliyordu.
3. Bu zincirde sonraki işlemlerden biri optimistic concurrency çakışmasına girebiliyordu.
4. Temel SKU kaydedilmiş olduğu için kullanıcı kısmi kayıt mesajı görüyordu.

Yeni davranış:

1. Ana SKU değişikliği `baseChanged=true` yapar.
2. Ürün çoklu varyantlı olduğu için kayıtlı varyantlar `changed=true` yapılmaz.
3. Yalnız `PUT /api/products/P00004` çağrılır.
4. Dört dokunulmamış varyant için ek `PUT` isteği oluşturulmaz.

Varyantsız üründe ise ana SKU tek varsayılan satış kaydının SKU'su olarak da kullanıldığı için hem temel ürün hem de tek kayıtlı varyant değişmiş kabul edilir.

## 9. Kısmi kayıt davranışı

Backend işlemleri tek bir frontend isteği içinde görünse de ürün, durum, varyant ve görsel farklı API endpoint'leri üzerinden kaydedilir. Bu nedenle bütün süreci kapsayan tek bir backend transaction yoktur.

Örnek:

1. Temel ürün başarıyla güncellenir.
2. İlk varyant başarıyla güncellenir.
3. İkinci varyant başarısız olur.

Bu durumda frontend başarılı ilk iki işlemi geri alınmış gibi göstermez. Action state aşağıdaki bilgileri taşır:

- Durum: `partial`
- Başarıyla tamamlanan işlemlerin özeti
- Başarısız olan kesin aşama
- Ürün kimliği
- Varsa backend takip kodu
- Varsa güvenli alan hataları

Henüz hiçbir mutation başarıyla tamamlanmadan hata oluşursa durum `partial` değil `error` olur.

Uzun varyant listelerinde hata mesajının okunabilir kalması için tamamlanan işlemler özetlenir; ilk iki işlem adı ve kalan işlem sayısı gösterilir.

## 10. `409 Conflict` ve optimistic concurrency davranışı

Frontend, `409 Conflict` cevabında aynı mutation'ı otomatik olarak tekrar çalıştırmaz. Kör tekrar denemesi, başka bir kullanıcı veya işlem tarafından yapılan güncel değişikliği ezme riski taşıdığı için uygulanmamıştır.

Güncelleme sırasında `409` alınırsa:

1. Hatanın oluştuğu aşama kaydedilir.
2. Daha önce tamamlanan işlem varsa sonuç `partial`, yoksa `error` olur.
3. `GET /api/products/{productId}` ile sunucudaki güncel kayıt yeniden okunmaya çalışılır.
4. Formdaki kullanıcının mevcut değerleri ekranda korunur.
5. Güncel kayıt başarıyla doğrulanırsa hata kutusunda **Güncel kaydı yükle** bağlantısı gösterilir.
6. Kullanıcı bu bağlantıya basarsa ürün sayfası `?reload=1` ile yeniden açılır.

`409` mesajında backend'in İngilizce teknik hata metni ana kullanıcı mesajı olarak tekrar edilmez. Kullanıcıya hangi aşamanın başka bir işlemle çakıştığı Türkçe ve işlem odaklı biçimde bildirilir. Backend `traceId` değeri varsa destek ve log takibi için korunur.

## 11. Başarı davranışı ve cache yenileme

Bütün gerekli işlemler tamamlandığında:

1. `/products` liste route'u yeniden doğrulanır.
2. `/products/{productId}` detay route'u yeniden doğrulanır.
3. Kullanıcı `/products/{productId}?saved=1` adresine yönlendirilir.

Bu işlemden sonra detay sayfası ürünü backend'den tekrar getirir. Ürün API çağrıları paylaşılan tarayıcı cache'ine güvenmez ve doğrulanmış Admin access token'ı yalnız server-side API client üzerinden kullanır.

## 12. Görsel güncelleme sınırı

Medya editörü şu anda en fazla 10 görsel için:

- Yerel dosya seçimi
- Tarayıcı içi önizleme
- Ana görsel seçimi
- Yeni seçilen yerel görseli önizlemeden kaldırma

özelliklerini sağlar.

Seçilen dosyalar henüz bir bulut depolama servisine yüklenmez ve ürün güncelleme isteğinde backend'e gönderilmez. `form-data.ts` ile `api.ts` URL tabanlı tek görsel DTO'sunu desteklemeye hazır olsa da mevcut medya UI'ı bu alanları üretmez. Bulut yükleme servisi bağlandığında dosya yükleme sonucu elde edilen URL'lerin ayrı görsel endpoint'ine gönderilmesi gerekir.

Mevcut kayıtlı görselleri silme veya ana görsel seçimini backend'de değiştirme işlemi de bu frontend güncelleme akışının mevcut kapsamında değildir.

## 13. Güvenlik sınırları

Ürün güncelleme mantığı yalnız UI'daki buton görünürlüğüne güvenmez:

- Server Action, form parse edilmeden önce aktif Admin oturumunu zorunlu tutar.
- Access token Client Component'e veya form alanlarına yazılmaz.
- Backend çağrıları server-only API katmanından yapılır.
- Ürün kimliği URL ve endpoint içinde `encodeURIComponent` ile güvenli biçimde kullanılır.
- Backend ProblemDetails cevabından yalnız güvenli başlık, detay, durum, takip kodu ve alan hataları action state'e taşınır.
- `409` durumunda otomatik overwrite veya sınırsız retry yapılmaz.

Nihai yetkilendirme ve veri bütünlüğü otoritesi backend'dir. Frontend değişiklik filtreleme mantığı güvenlik kontrolü değil, doğru mutation niyetini ve daha güvenli kullanıcı deneyimini sağlar.

## 14. Test kapsamı

`form-data.test.ts` içinde güncelleme davranışı için eklenen regresyon testleri şunlardır:

1. Dokunulmamış kayıtlı varyantın edit mutation listesinden çıkarılması.
2. `changed=on` taşıyan kayıtlı varyantın güncelleme listesine alınması.
3. P00004 benzeri dört varyantlı üründe yalnız ana SKU değiştiğinde temel ürünün değişmiş sayılması ve dört varyantın gönderilmemesi.
4. Çaprazlanan varyantlardan biri dolu, diğeri tamamen boş olduğunda yalnız dolu satırın oluşturma payload'ına alınması.

Genel ürün formu testleri ayrıca şu alanları kapsar:

- Tekli ve çoklu varyant parse işlemleri
- Birleşik seçenek adı/değeri
- Tekrarlanan kombinasyonların reddedilmesi
- Boş otomatik varyantların atlanması
- Yarım doldurulmuş varyantların doğrulanması
- Karşılaştırma fiyatı kuralı
- Tekrarlanan form alanlarından tag oluşturulması

Son doğrulama sonuçları:

| Kontrol | Sonuç |
| --- | --- |
| `pnpm test` | 6 test dosyası ve 28 test başarılı |
| `pnpm typecheck` | Başarılı |
| `pnpm lint` | Başarılı |
| `pnpm build` | Next.js production build başarılı |

## 15. Bilinen sınırlar ve gelecekteki backend çalışması

Mevcut frontend çözümü gereksiz istekleri ve bu isteklerden doğan çakışma ihtimalini azaltır; bütün eşzamanlılık problemlerini ortadan kaldıran bir backend concurrency protokolü değildir.

Gelecekte değerlendirilebilecek backend geliştirmeleri:

- Ürün detay DTO'sunda açık `rowVersion`, ETag veya benzeri concurrency token dönülmesi
- Güncelleme isteklerinde `If-Match` veya açık version alanı kullanılması
- Ürün, durum ve varyant değişikliklerini tek transaction/komut altında atomik uygulayan toplu güncelleme endpoint'i
- `409` cevabında hangi entity ve version'ın çakıştığını güvenli bir hata koduyla bildirme
- Varyant silme veya birleştirme sözleşmesi
- Çoklu ürün görseli yükleme, silme, sıralama ve ana görsel değiştirme endpoint akışı

Bu backend özellikleri eklenene kadar frontend aşağıdaki politikayı sürdürmelidir:

1. Yalnız kullanıcı tarafından değiştirilen kayıtları göndermek.
2. Mutation'ları kontrolsüz paralel çalıştırmamak.
3. `409` cevabında otomatik overwrite yapmamak.
4. Kısmi başarıyı tam başarı gibi göstermemek.
5. Güncel kaydı yeniden yükleme kararını kullanıcıya bırakmak.

## 16. Bakım sırasında dikkat edilecek noktalar

Ürün formuna yeni alan eklendiğinde alanın hangi endpoint'e ait olduğu açıkça belirlenmelidir:

- Temel ürün DTO'suna aitse `baseProductFieldNames` değişiklik takibine eklenmelidir.
- Ayrı bir `PATCH` endpoint'ine aitse eski değer ile yeni değer karşılaştırılmalıdır.
- Varyant alanıysa `DraftField` listesine ve `changed` işaretleme akışına eklenmelidir.
- Yeni varyantın anlamlı sayılmasını sağlayacak bir satış alanıysa `isBlankNewVariant` kontrolüne eklenmelidir.
- Backend payload'ına ait olmayan UI niyet alanları API çağrısından önce çıkarılmalıdır.
- Yeni mutation aşaması `failedOperation` ve `completedOperations` takibine dahil edilmelidir.
- Yeni hata davranışı en az bir başarı, sıfır başarı ve `409` senaryolarıyla test edilmelidir.

Bu kurallar korunmadığında düzenleme formu yeniden bütün varyantları gönderen eski davranışa dönebilir veya kullanıcıya hatalı tam başarı mesajı gösterebilir.
