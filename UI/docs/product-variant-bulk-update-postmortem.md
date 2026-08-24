# Ürün varyantı güncelleme ve SKU takası: kök neden, çözüm ve öğrenilenler

Bu belge, Admin ürün düzenleme ekranında karşılaşılan varyant güncelleme sorunlarının nasıl teşhis edildiğini ve kalıcı olarak nasıl çözüldüğünü öğretici bir vaka incelemesi olarak anlatır.

- Tarih: 22 Ağustos 2026
- Kapsam: ELEVEN API ve Admin UI ürün varyantı düzenleme akışı
- Ana senaryo: İki mevcut varyantın SKU değerlerini birbiriyle değiştirmek
- Sonuç: Mevcut varyantlar atomik bulk endpoint ile, yeni varyantlar bağımsız POST istekleriyle kaydediliyor

## 1. Başlangıçta görülen belirtiler

Sorun tek bir hatadan oluşmuyordu. Aynı kullanıcı akışında farklı katmanlara ait üç problem art arda görünür hale geldi.

### 1.1 Mevcut varyantın yeniden oluşturulmaya çalışılması

Ürün detayında örneğin yedi kayıtlı varyant bulunmasına rağmen düzenleme ekranında yalnız dört mantıksal kombinasyon gösterilebiliyordu. Aynı seçenek kombinasyonuna bağlı birden fazla eski kayıt map içinde birbirini eziyor, bazı varyant kimlikleri form state'inden kayboluyordu.

Kimliği kaybolan satır güncelleme sırasında mevcut kayıt olarak değil, yeni varyant olarak algılanıyordu:

- `id` varsa: mevcut varyant güncellenmelidir.
- `id` yoksa: yeni varyant oluşturulmalıdır.

Admin tarafında kombinasyon başına tek kayıt tutmak yerine kayıt dizisi tutuldu. Böylece aynı mantıksal kombinasyona bağlı bütün kalıcı kimlikler korundu ve otomatik Kartezyen çaprazlama sistemi bozulmadan devam etti.

İlgili kod:

- [`variant-editing.ts`](../admin/src/modules/products/variant-editing.ts)
- [`variant-editor.tsx`](../admin/src/modules/products/components/variant-editor.tsx)

### 1.2 Gerçek olmayan concurrency hatası

Varyantsız bir ürün ilk kez `Uzunluk / 40 CM` gibi gerçek bir seçeneğe dönüştürülürken API şu hatayı veriyordu:

```text
409 concurrency_conflict
Entity: ProductVariantOptionValue
State: Modified
```

Bu hata başka bir yöneticinin aynı kaydı değiştirmesinden kaynaklanmıyordu. Yeni oluşturulan `ProductVariantOptionValue` bağlantısının GUID kimliği uygulama tarafından üretiliyordu; fakat EF Core konfigürasyonunda bu açıkça belirtilmemişti. EF yeni child entity'yi `Added` yerine `Modified` kabul ederek var olmayan bir satıra `UPDATE` göndermeye çalışıyordu. Sıfır satır etkilenince `DbUpdateConcurrencyException` oluşuyordu.

Çözüm, client-generated kimliği EF modelinde açıkça tanımlamak oldu:

```csharp
builder.Property(item => item.Id)
    .ValueGeneratedNever();
```

İlgili kod:

- [`ProductVariantOptionValueConfiguration.cs`](../../API/src/ECommerce.Persistence/Configurations/ProductVariantOptionValueConfiguration.cs)

Bu vaka önemli bir ayrımı gösterir: HTTP `409 concurrency_conflict` görülmesi, her zaman gerçekten iki kullanıcının aynı veriyi değiştirdiği anlamına gelmez. Önce EF exception'a katılan entity, entity state ve concurrency token bilgileri incelenmelidir.

### 1.3 İki SKU'nun birbiriyle değiştirilememesi

EF child entity sorunu çözüldükten sonra iki mevcut varyantın SKU değerleri takas edilmek istendi:

| Varyant | Başlangıç SKU | Hedef SKU |
| --- | --- | --- |
| Uzunluk: 45 CM | `SKU-A` | `SKU-B` |
| Uzunluk: 50 CM | `SKU-B` | `SKU-A` |

Admin her varyant için ayrı `PUT /api/product-variants/{id}` gönderiyordu. İlk istek hangi varyant için gönderilirse gönderilsin hedef SKU hâlâ diğer satıra ait olduğu için global unique index isteği reddediyordu.

İlk olarak 45 CM güncellenirse:

```text
45 CM: SKU-A -> SKU-B
50 CM: hâlâ SKU-B
Sonuç: duplicate SKU
```

İlk olarak 50 CM güncellenirse:

```text
50 CM: SKU-B -> SKU-A
45 CM: hâlâ SKU-A
Sonuç: duplicate SKU
```

Dolayısıyla istek sırasını ters çevirmek çözüm değildir.

## 2. Neden bazı kolay görünen çözümler kullanılmadı?

### 2.1 `Promise.all` kullanmak

İki tekil PUT isteğini paralel göndermek atomiklik sağlamaz. Her istek ayrı transaction'dır ve unique index her transaction içinde anında uygulanır. Sonuç yarış durumuna bağlı olur; iki isteğin birlikte başarılı olacağı garanti edilmez.

### 2.2 Admin tarafında geçici üçüncü SKU kullanmak

Teorik olarak istemci şu sırayı uygulayabilirdi:

1. `SKU-A` değerini geçici bir değere taşı.
2. `SKU-B` değerini `SKU-A` yap.
3. Geçici değeri `SKU-B` yap.

Bu yaklaşım güvenli değildir:

- Üç ayrı HTTP işlemi arasında kısmi kayıt oluşabilir.
- Geçici SKU başka istekler tarafından görülebilir.
- Network retry hangi adımın tamamlandığını belirsiz hale getirir.
- Stok hareketi ve concurrency token gibi yan etkiler birden fazla kez üretilebilir.
- İş kuralı ve transaction sorumluluğu istemciye sızar.

Geçici değer gerekiyorsa bunu transaction sahibi olan API yönetmelidir.

### 2.3 Unique kontrolünü kaldırmak

SKU global benzersizliği gerçek bir iş kuralıdır. Unique index'i kaldırmak veya kontrolü atlamak takası kolaylaştırır gibi görünür, fakat katalog ve stok kimliğinin güvenilirliğini bozar. Çözüm kuralı kaldırmak değil, değişikliği atomik yapmaktır.

## 3. API çözümü: atomik bulk update

Mevcut varyantlar için aşağıdaki endpoint eklendi:

```http
PUT /api/product-variants/by-product/{productId}/bulk
```

Bu endpoint yalnız mevcut varyantları günceller. Yeni varyant oluşturmaz.

Güncel sözleşme:

- [`PUT--api-product-variants-by-product--product-d--bulk.md`](api/api-project-docs/08-endpoint-sozlesmeleri/03-katalog-ve-etkilesim/PUT--api-product-variants-by-product--product-d--bulk.md)
- [`openapi-controller-contract.json`](api/api-project-docs/openapi-controller-contract.json)

### 3.1 Request'in önemli alanları

Her mevcut varyant tam hedef durumuyla birlikte gönderilir:

```json
{
  "variants": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "name": "Uzunluk",
      "value": "45 CM",
      "sku": "SKU-B",
      "price": 899.90,
      "stock": 5,
      "compareAtPrice": null,
      "barcode": null,
      "material": null,
      "isActive": true,
      "stockAdjustmentReason": "Admin toplu güncelleme",
      "expectedConcurrencyToken": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
    }
  ]
}
```

Buradaki değerler fark değil, hedef durumdur. Örneğin `stock: 5`, stoğa beş ekle anlamına gelmez; nihai stok bakiyesinin beş olması demektir.

### 3.2 Transaction içindeki iki aşamalı SKU değişimi

SQL Server unique index nedeniyle SKU takası iki kayıt aşamasında yapılır. İki aşama da aynı serializable transaction içindedir.

```text
Başlangıç
  45 CM = SKU-A
  50 CM = SKU-B

1. aşama: yalnız transaction içinde görülen geçici değerler
  45 CM = __BULK__<benzersiz-guid-1>
  50 CM = __BULK__<benzersiz-guid-2>

2. aşama: nihai değerler
  45 CM = SKU-B
  50 CM = SKU-A

Commit
```

İkinci aşamada veya başka bir doğrulamada hata oluşursa transaction rollback edilir. Dışarıdan ne geçici SKU ne de yarım güncellenmiş veri görülür.

İlgili handler:

- [`BulkUpdateProductVariantsCommandHandler.cs`](../../API/src/ECommerce.Application/Products/Variants/Commands/BulkUpdateProductVariants/BulkUpdateProductVariantsCommandHandler.cs)

### 3.3 Bulk öncesi doğrulamalar

API değişiklik yapmadan önce şunları doğrular:

- Batch 1–100 satır arasındadır.
- Varyant kimlikleri batch içinde tekrar etmez.
- Hedef SKU değerleri batch içinde tekrar etmez.
- Bütün varyantlar path'teki ürüne aittir.
- Hedef SKU değerleri batch dışındaki varyantlarla çakışmaz.
- Her `expectedConcurrencyToken` veritabanındaki güncel tokenla eşleşir.
- Fiyat, stok, barkod, materyal ve seçenek parçaları geçerlidir.

Batch içindeki mevcut SKU sahipleri dış çakışma kabul edilmez. Bu kural `A ↔ B` takasını ve `A → B, B → C, C → A` gibi daha uzun döngüleri mümkün kılar.

### 3.4 Optimistic concurrency token neden gereklidir?

`expectedConcurrencyToken`, varyant kimliği değildir. Varyant her başarılı değiştiğinde yenilenen sürüm değeridir.

Akış şöyledir:

1. Admin GET response'undan güncel `concurrencyToken` değerini alır.
2. Kullanıcı varyantı düzenler.
3. Admin bu değeri `expectedConcurrencyToken` olarak bulk request'e koyar.
4. API tokenı veritabanındaki sürümle karşılaştırır.
5. Başarılı response yeni `concurrencyToken` döndürür.
6. Admin bir sonraki mutation için yeni tokenı saklar.

Eski tokenla gelen istek bütün batch için `409 concurrency_conflict` üretir. API tokenı körlemesine yenileyip otomatik retry yapmaz; çünkü kullanıcının değişikliği güncel veri üzerinde yeniden değerlendirmesi gerekir.

## 4. Admin UI çözümü

Admin form parser'ı değişen varyantları iki gruba ayırır:

```text
Değişen varyantlar
├── id taşıyan mevcut kayıtlar
│   └── tek bulk PUT
└── id taşımayan yeni kayıtlar
    └── her biri için bağımsız POST
```

### 4.1 Mevcut varyantlar

Bütün değişen mevcut varyantlar tek payload içinde şu endpoint'e gönderilir:

```http
PUT /api/product-variants/by-product/{productId}/bulk
```

Artık mevcut varyantlar için tek tek `PUT /api/product-variants/{id}` döngüsü çalıştırılmaz. Bu sayede SKU takası API transaction'ı içinde çözülür.

İlgili kod:

- [`api.ts`](../admin/src/modules/products/api.ts)
- [`actions.ts`](../admin/src/modules/products/actions.ts)

### 4.2 Yeni varyantlar

Bulk endpoint yeni kayıt kabul etmediği için `id` taşımayan kombinasyonlar şu endpoint ile oluşturulur:

```http
POST /api/product-variants/by-product/{productId}
```

Birden fazla yeni varyant tek bir atomik batch değildir. Her POST bağımsız hata sınırında çalışır:

```ts
for (const variant of newVariants) {
  try {
    await createProductVariant(productId, variant, session);
    completedOperations.push(variantLabel);
  } catch (error) {
    operationFailures.push({ variantLabel, error });
  }
}
```

Bir yeni varyant hata verdiğinde sonraki varyantların oluşturulmasına devam edilir. Sonuç ekranında iki ayrı liste gösterilir:

- Kaydedilenler
- Tamamlanamayanlar

Bu davranış bilinçli olarak bulk update'ten farklıdır:

| İşlem | Endpoint | Atomiklik |
| --- | --- | --- |
| Mevcut varyant güncelleme | Bulk PUT | Bütün mevcut varyantlar birlikte başarılı veya rollback |
| Yeni varyant oluşturma | Tekil POST | Her yeni varyant bağımsız başarılı veya başarısız |

### 4.3 Tokenın form state'inde korunması

Generated `ProductVariantDto` artık zorunlu `concurrencyToken` içerir. Admin bu tokenı görünmeyen form alanında taşır:

```html
<input
  type="hidden"
  name="variants.0.expectedConcurrencyToken"
  value="..."
/>
```

Başarılı bulk response'taki authoritative varyantlar ve yeni tokenlar form state'ine yazılır. Editör revision hesabı tokenı da içerdiği için sonraki submit eski tokenı tekrar kullanmaz.

İlgili kod:

- [`variant-editor.tsx`](../admin/src/modules/products/components/variant-editor.tsx)
- [`form-data.ts`](../admin/src/modules/products/form-data.ts)
- [`variant-editing.ts`](../admin/src/modules/products/variant-editing.ts)

### 4.4 API hata indeksinin gerçek form satırına çevrilmesi

Admin yalnız değişen varyantları bulk request'e ekler. Bu nedenle API'deki batch indeksi ile ekrandaki satır indeksi farklı olabilir.

Örnek:

```text
Ekranda değişen satırlar: 2 ve 5
Bulk payload indeksleri: 0 ve 1
API hatası: variants[1].sku
Form alanı: variants.5.sku
```

Admin API hata anahtarını gerçek form indeksine çevirerek mesajı doğru SKU inputunun altında gösterir.

İlgili kod:

- [`action-error.ts`](../admin/src/modules/products/action-error.ts)

## 5. Hata kodlarını doğru yorumlama

| HTTP/code | Anlamı | Admin davranışı |
| --- | --- | --- |
| `409 product_variant_sku_conflict` | Hedef SKU batch dışındaki başka bir varyantta kullanılıyor | API `errors` alanını ilgili SKU inputlarına bağla |
| `409 concurrency_conflict` | En az bir varyant kullanıcı formu açtıktan sonra değişmiş | Güncel veriyi yeniden getir, otomatik overwrite yapma |
| `400 validation_error` | Payload alanlarından veya batch kurallarından biri geçersiz | Alan hatalarını ilgili kontrollere bağla |
| `404 resource_not_found` | Varyant bulunamadı veya path'teki ürüne ait değil | Güncel ürün detayını yeniden yükle |
| `500 internal_error` | Beklenmeyen sunucu hatası | Güvenli genel mesaj ve varsa trace ID göster |

SKU çakışması ile concurrency çatışmasını aynı mesaj altında toplamak hatalıdır. Birincisi hedef verinin iş kuralına uymaması, ikincisi formun eski sürüm üzerinden işlem yapmasıdır.

## 6. Başarı sonrası “Kaydediliyor…” durumunun kapanmaması

API kayıtları başarıyla tamamladıktan sonra ürün formu bir süre `Kaydediliyor…` durumunda kalıyordu. Bu, persistence sorunu değil React form yaşam döngüsü sorunuydu.

Ürün formu submit durumunu iç bileşendeki `useFormStatus` üzerinden dolaylı okuyordu. Başarı sonrası route replace/refresh akışı sırasında bu pending görünümü takılı kalabiliyordu. Form, diğer Admin formlarıyla aynı şekilde `useActionState` tarafından döndürülen authoritative pending değerine geçirildi:

```ts
const [state, formAction, actionPending] = useActionState(
  action,
  initialProductActionState,
);
```

Görsel yükleme aşaması ayrı pending state olarak korunur. Böylece buton yalnız Server Action veya medya işlemi gerçekten devam ederken `Kaydediliyor…` gösterir.

İlgili kod:

- [`product-form.tsx`](../admin/src/modules/products/components/product-form.tsx)

## 7. Uçtan uca nihai akış

```text
Kullanıcı formu açar
  ↓
GET ProductVariantDto + concurrencyToken
  ↓
Admin otomatik seçenek kombinasyonlarını ve kalıcı id/token değerlerini korur
  ↓
Kullanıcı varyantları değiştirir
  ↓
Form parser yalnız değişen satırları seçer
  ↓
┌─────────────────────────────┬─────────────────────────────┐
│ Mevcut varyantlar (id var)  │ Yeni varyantlar (id yok)    │
├─────────────────────────────┼─────────────────────────────┤
│ Tek atomik bulk PUT         │ Bağımsız POST döngüsü       │
│ SKU takası desteklenir      │ Bir hata sonrakini durdurmaz│
└─────────────────────────────┴─────────────────────────────┘
  ↓
Başarılı ve başarısız işlemler ayrı toplanır
  ↓
Bulk response'taki yeni tokenlar form state'ine yazılır
  ↓
Authoritative ürün detayı yeniden okunur
```

## 8. Testlerle korunan davranışlar

Admin testleri şu kritik davranışları sabitler:

- İki mevcut SKU takasının tek bulk PUT olarak gönderilmesi
- Bulk request'te doğru `expectedConcurrencyToken` değerlerinin bulunması
- Bulk response'taki yeni tokenların form state'inde saklanması
- Mevcut varyantlar için eski tekil PUT döngüsünün çalışmaması
- Yeni varyantların tekil POST endpoint'inde kalması
- Bir yeni varyantın hatasının sonraki POST'u durdurmaması
- Başarılı ve başarısız yeni varyantların ayrı sonuç listelerine girmesi
- API `variants[n].sku` alanlarının gerçek form satırına çevrilmesi
- Concurrency token değiştiğinde editör revision değerinin yenilenmesi
- Kayıtlı, değişen varyantın token olmadan mutation'a gönderilmemesi

İlgili testler:

- [`api.test.ts`](../admin/src/modules/products/api.test.ts)
- [`actions.test.ts`](../admin/src/modules/products/actions.test.ts)
- [`action-error.test.ts`](../admin/src/modules/products/action-error.test.ts)
- [`form-data.test.ts`](../admin/src/modules/products/form-data.test.ts)
- [`variant-editing.test.ts`](../admin/src/modules/products/variant-editing.test.ts)

Son doğrulamada:

- OpenAPI generated type drift kontrolü geçti.
- TypeScript typecheck geçti.
- Lint geçti.
- 40 test dosyasında 162 test geçti.
- Admin production build geçti.

## 9. Bu vakadan çıkarılacak genel dersler

1. **Kimlik korunmadan update yapılamaz.** Dinamik form ve Kartezyen kombinasyon üretiminde kalıcı `id` değerleri kaybolursa istemci update yerine create niyeti üretir.
2. **Her 409 gerçek concurrency değildir.** EF entity state ve exception entry bilgileri incelenmelidir.
3. **Unique alan takası tekil isteklerle güvenli değildir.** Değişiklik kümesinin sahibi olan API atomik transaction sağlamalıdır.
4. **İstemci transaction orkestratörü olmamalıdır.** Geçici SKU gibi iç detaylar API sınırında kalmalıdır.
5. **Bulk ve create aynı semantiğe sahip değildir.** Mevcut kayıtlar birlikte atomik güncellenirken yeni kayıtlar sözleşmeye göre bağımsız oluşturulabilir.
6. **Concurrency token response'tan sonraki request'e taşınmalıdır.** Token yalnız hata kontrolü değil, mutation protokolünün bir parçasıdır.
7. **Filtrelenmiş payload indeksleri UI indeksleri değildir.** Alan hataları kullanıcıya gösterilmeden önce gerçek form satırlarına çevrilmelidir.
8. **Başarılı persistence ile UI pending state farklı problemlerdir.** Veritabanı güncellendiği halde buton takılıysa Server Action yaşam döngüsü ayrıca incelenmelidir.

## 10. Benzer bir sorun tekrarlandığında kontrol listesi

1. İstek create mi, update mi? Payload içinde kalıcı `id` var mı?
2. Güncellenen DTO güncel `concurrencyToken` taşıyor mu?
3. Admin eski tokenı mı, son response tokenını mı gönderiyor?
4. İşlem unique alanlar arasında takas veya döngü içeriyor mu?
5. Tekil endpoint yerine belgelenmiş atomik bulk endpoint kullanılmalı mı?
6. API hatası `product_variant_sku_conflict` mi, `concurrency_conflict` mi?
7. ProblemDetails `errors` indeksi gerçek form satırına doğru çevriliyor mu?
8. Yeni kayıtlar mevcut varyant bulk payload'ına yanlışlıkla ekleniyor mu?
9. Bağımsız POST hatası sonraki yeni varyantı gereksiz yere durduruyor mu?
10. Başarı response'undaki yeni token form state'ine yazılıyor mu?

Bu kontrol listesiyle sorun UI kimlik eşlemesi, API transaction tasarımı, EF persistence veya React pending state katmanlarından hangisindeyse daha hızlı ayrıştırılabilir.
