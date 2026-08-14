# Storefront MVP Kalan İşler

**Tarih:** 11 Ağustos 2026  
**Kapsam:** Ana sayfa tasarımı bu belgenin dışındadır. Bu liste katalog, ürün detayı, sepet, checkout, SEO, performans, güvenlik ve canlıya hazırlık işlerini kapsar.

## Öncelik sırası

| Kimlik | İş | Öncelik | Alan | Durum |
| --- | --- | --- | --- | --- |
| SF-001 | Checkout hydration mismatch sorununu düzelt | Kritik | Storefront | UI tamamlandı; browser console doğrulaması bekliyor |
| SF-002 | Ürün ve katalog soft-404 cevaplarını gerçek `404` yap | Kritik | Storefront / SEO | Katalog doğrulandı; ürün API açıkken doğrulanacak |
| SF-003 | Sepet ve sipariş satırlarına varyant adı/değeri snapshot'ı ekle | Yüksek | API + Storefront | API bekleniyor |
| SF-004 | Ödeme gelene kadar production sipariş oluşturmayı kapalı tut | Yüksek | Yayın yapılandırması | Tamamlandı ve production build üzerinde doğrulandı |
| SF-005 | Guest cart `Set-Cookie` aktarımını allowlist ile sertleştir | Orta | Storefront BFF | Tamamlandı; birim testleri geçti |
| SF-006 | Filtre formundaki boş query parametrelerini temizle | Orta | Katalog / SEO | Tamamlandı ve runtime'da doğrulandı |
| SF-007 | Çok görselli ürün fixture'ı ile mobil galeriyi doğrula | Orta | Ürün / Mobil | Üç görselli bileşen testi geçti; gerçek API fixture'ı bekleniyor |
| SF-008 | Kalıcı kritik akış ve erişilebilirlik regresyon testlerini kur | Orta | Test | Kısmen tamamlandı; browser E2E/axe ve gerçek screen reader bekleniyor |
| SF-009 | Canlıya uygun kargo ve katalog içeriklerini doğrula | Orta | İçerik / Operasyon | Bekliyor |
| SF-010 | Gerçek domain üzerinde SEO ve saha performans doğrulaması yap | Yayın öncesi | SEO / Performans | Deployment bekleniyor |

## API Tarafında Yapılması Gerekenler

Bu maddeler UI içinde doğru ve performanslı biçimde üretilemez; API sözleşmesi, OpenAPI ve ilgili endpoint Markdown dokümanı birlikte güncellenmelidir. Ayrıntılı ve kalıcı takip belgesi `UI/API-IMPROVEMENT-RECOMMENDATIONS.md` dosyasıdır.

1. **API-001 — Yayımlanmış katalog facetleri:** Marka, koleksiyon ve ürün türü seçeneklerini yayımlanmış ürün adetleriyle tek istekte döndür. Sıfır sonuçlu seçenekleri varsayılan sonuçtan çıkar; diğer aktif filtrelere göre facet sayılarını yeniden hesapla.
2. **API-002 — Ürün türleri OpenAPI cevabı:** `GET /api/product-types` için gerçek sayfalı `200` response şemasını, `POST` için gerçek `201 ProductTypeDto` cevabını üret. Bu tamamlandığında Storefront'taki elle yazılmış `ProductTypePage` tipi kaldırılacak.
3. **API-003 — Varyant snapshot'ı:** `CartItemDto` ve `OrderItemDto` içinde seçilen varyantın adı ile değerini ayrı ve kayıpsız snapshot alanları olarak döndür. Mevcut OpenAPI hâlâ cart için yalnız `variantName`, order için yalnız `variantSku` taşıyor; Storefront ürün detayına N+1 istek atmayacak.
4. **API-004 — Sınıflandırma URL çözümleme:** Marka, koleksiyon ve ürün türü landing sayfaları için kararlı public `url` alanı ve URL ile tekil çözümleme endpointleri sağla. Böylece Storefront ilk 100 sınıflandırma kaydını indirerek ID çözmek veya ürün türü adından slug tahmin etmek zorunda kalmayacak.

## 12 Ağustos 2026 UI Uygulama ve Doğrulama Sonucu

### Tamamlanan UI düzeltmeleri

- Cart indicator, sepet ve checkout ilk render'ı ortak loading durumundan başlatıldı; modül seviyesindeki client snapshot'ın SSR/hydration ağacını değiştirmesi engellendi.
- Ödeme entegrasyonu gelene kadar production ortamında sipariş oluşturma hem form hem BFF route seviyesinde kodla kapatıldı. Production build üzerinde endpoint `503 checkout_unavailable` döndürdü.
- Cart ve checkout BFF cookie aktarımı ortak allowlist sınırına taşındı. Bilinmeyen cookie, geçersiz token ve upstream `Domain/Path/SameSite` değerleri kabul edilmiyor; izin verilen cookie Storefront origin'i için `HttpOnly`, `SameSite=Lax`, `Path=/` ve production'da `Secure` olarak yeniden oluşturuluyor.
- Katalogdaki boş/default/geçersiz filtre parametreleri veri isteğinden önce temiz URL'ye yönlendiriliyor. `brand=&collection=&type=` production runtime'da `307 /products` verdi.
- Türkçe, responsive ve klavyeyle kullanılabilir `not-found.tsx` eklendi. Streaming'i 200'e sabitleyen ürün/katalog/root loading sınırları kaldırıldı.
- Üç görselli galeri için sıralama, üç slide, 4:5 geometri, tek preload ve ikincil lazy-load davranışını kalıcı test eden fixture eklendi.
- Sepet UI'si API ileride `variantValue` sağladığında `Varyant adı · Varyant değeri` gösterecek biçimde hazırlandı; API sözleşmesi tamamlanana kadar N+1 ürün isteği eklenmedi.

### Doğrulama kanıtı

- `pnpm lint`: geçti.
- `pnpm typecheck`: geçti.
- `pnpm test`: 17 dosyada 55 test geçti.
- `pnpm api:types:check`: geçti.
- `pnpm build`: Next.js 16.2.12 production build geçti.
- Runtime: `/`, `/products`, `/cart`, `/checkout` `200`; `/products?page=999` gerçek `404`, özel Türkçe içerik ve `noindex`; boş filtre query'si temiz URL'ye yönlendirildi.
- Production checkout BFF: `POST /api/cart/checkout/guest` ödeme kapalıyken `503` ve `checkout_unavailable` döndürdü.

### Açık doğrulamalar

- Yerel API `localhost:3300` bu kontrolde ayağa kalkmadığı için olmayan ürün slug'ının gerçek `404` cevabı runtime'da doğrulanamadı. UI tarafında streaming sınırı kaldırıldı; API açıkken test yeniden çalıştırılmalı.
- Bu oturumda bağlı tarayıcı bulunmadığından desktop/mobile görsel regresyon, console ve network paneli yeniden çalıştırılamadı.
- Gerçek API'den gelen en az üç görselli ürün fixture'ı, axe/browser E2E, gerçek screen reader, gerçek cihaz ve production domain CWV/SEO kontrolleri henüz tamamlanmadı.

## SF-001 — Checkout hydration mismatch

### Problem

Checkout bileşeni ilk client render'ında modül seviyesindeki sepet snapshot'ını kullanabiliyor. Sunucu loading görünümü üretirken client hazır sepet görünümüyle başlayabildiği için React hydration mismatch oluşuyor.

### Yapılacaklar

- [x] Checkout'ın sunucu ve client ilk render durumunu deterministik hale getir.
- [x] Sepet snapshot paylaşımını hydration-safe bir yapıya taşı; gerekirse `useSyncExternalStore` için sabit server snapshot kullan.
- [ ] Mevcut cart indicator, cart sayfası ve checkout aboneliklerinin birbirini yarış durumuna sokmadığını doğrula.
- [ ] Loading görünümünden hazır forma geçişte odak, form girdileri ve ekran okuyucu duyurularını koru.
- [x] Bu davranış için kalıcı regresyon testi ekle.

### Kabul ölçütleri

- Next DevTools ve browser console içinde hydration uyarısı bulunmuyor.
- Checkout doğrudan açıldığında ve sepetten client navigation ile gelindiğinde aynı sonucu veriyor.
- Sepet özeti API'nin son authoritative cevabıyla eşleşiyor.
- Gereksiz çift cart isteği veya görünür loading/form sıçraması oluşmuyor.

## SF-002 — Gerçek 404 ve Türkçe not-found deneyimi

### Problem

Olmayan ürün ve var olmayan ileri katalog sayfası 404 ekranı gösterse de HTTP `200` dönüyor. Bu davranış arama motorları için soft-404 riski oluşturuyor. Ayrıca mevcut ekran Next.js'in varsayılan İngilizce 404 görünümü.

### Yapılacaklar

- [x] Olmayan ürün kararının response streaming başlamadan verilmesini sağla.
- [x] İçeriği olmayan ileri katalog sayfasının gerçek HTTP `404` dönmesini sağla.
- [x] Storefront diline ve tasarım sistemine uygun `not-found.tsx` oluştur.
- [x] Not-found görünümüne katalog ve ana sayfa için gerçek geri dönüş bağlantıları ekle.
- [x] Katalog not-found sayfasının `noindex` kaldığını ve sitemap'e girmediğini doğrula; ürün runtime kontrolü API açıkken tekrarlanacak.
- [ ] Ürün `404`, pasif ürün ve katalog `page=999` regresyon testlerini ekle.

### Kabul ölçütleri

- Olmayan ürün ve olmayan katalog sayfası HTTP `404` döndürüyor.
- Render edilen sayfa Türkçe, mobil uyumlu ve klavyeyle kullanılabilir.
- Yanlış canonical veya Product JSON-LD üretilmiyor.
- Sitemap yalnız başarılı ve indexlenebilir ürün URL'lerini içeriyor.

## SF-003 — Varyant snapshot API sözleşmesi

### Problem

Kullanıcı `Renk · Pudra` seçtiğinde cart cevabında yalnız `Renk` dönüyor. `CartItemDto` varyant değerini, `OrderItemDto` ise müşteriye gösterilecek değişmez varyant adı/değeri snapshot'ını taşımıyor.

### Yapılacaklar

- [ ] API tarafında `API-003` önerisini controller, DTO, OpenAPI ve Markdown dokümanlarında uygula.
- [ ] `CartItemDto` içinde `variantName` ve `variantValue` veya belgeli eşdeğer görüntüleme sözleşmesini sağla.
- [ ] `OrderItemDto` içinde sipariş anındaki değişmez varyant adı/değeri snapshot'ını sağla.
- [ ] Güncellenen OpenAPI'den Storefront tiplerini yeniden üret.
- [ ] Sepet ve sipariş onayında teknik SKU yerine müşteri tarafından seçilen seçeneği göster.
- [ ] Varyantsız ürünlerde `Default/Varsayılan` gibi teknik değerlerin görünmediğini doğrula.

### Kabul ölçütleri

- `Renk · Pudra` sepet, checkout özeti ve sipariş detayında kayıpsız görünüyor.
- Frontend ürün detayına N+1 istek yapmıyor.
- Ürün varyantı sonradan değişse bile geçmiş sipariş satırının snapshot'ı değişmiyor.

Takip belgesi: `UI/API-IMPROVEMENT-RECOMMENDATIONS.md`, kayıt `API-003`.

## SF-004 — Ödeme gelene kadar sipariş oluşturma kapısı

### Yapılacaklar

- [x] Production'da env yanlışlıkla true olsa bile sipariş oluşturmanın kapalı kaldığını doğrula.
- [x] Ödeme entegrasyonu tamamlanmadan production'da açılamayacak kod seviyesi kontrol oluştur.
- [x] Kapalı durumda butonun disabled, okunabilir ve nedenini açıklayan durumda olduğunu doğrula.
- [x] Yerel test ortamında açılan sipariş oluşturma davranışını production ayarından ayır.
- [ ] Ödeme entegrasyonu geldiğinde sipariş, payment intent, pending/paid/failed ve idempotency akışını yeniden tasarla ve test et.

### Kabul ölçütleri

- Production'da ödeme entegrasyonu yokken sipariş mutation'ı UI ve BFF seviyesinde kapalıdır.
- Kullanıcı bilgilerini doldurabilse bile yanlışlıkla sipariş oluşturamaz.
- Açma işlemi yalnız belgeli ödeme akışı ve yayın onayıyla yapılır.

## SF-005 — Guest cart cookie sertleştirmesi

### Yapılacaklar

- [x] Cart BFF'de upstream `Set-Cookie` değerlerini doğrudan geçirmek yerine yalnız belgeli guest cookie adlarını kabul et.
- [x] Token formatını doğrula ve güvenli cookie niteliklerini Storefront origin'i altında yeniden yaz.
- [x] Birden fazla `Set-Cookie` başlığını kaybetmeden işle.
- [ ] Bilinmeyen cookie, geçersiz token, Domain/Path manipülasyonu ve timeout testleri ekle.

### Kabul ölçütleri

- Browser yalnız izin verilen `HttpOnly`, production'da `Secure`, `SameSite=Lax` guest cookie'lerini alır.
- Cookie değeri client JavaScript, DOM, log veya rapora sızmaz.
- Origin kontrolü olmayan mutation hâlâ `403` döndürür.

## SF-006 — Temiz filtre URL'leri

### Problem

GET filtre formu seçilmeyen alanları `brand=&collection=&type=` biçiminde URL'ye ekleyebiliyor. Canonical bu değerleri temizlese de kullanıcıya görünen ve paylaşılan URL gereksiz parametreler taşıyor.

### Yapılacaklar

- [x] Boş filtre değerlerini navigasyon URL'sinden kaldır.
- [x] Filtre değiştiğinde sayfayı `1`e döndür.
- [x] Sıralama, filtre ve sayfalama canonical/noindex politikasını koru.
- [x] Filtre temizleme ve tek tek aktif filtre kaldırma testlerini genişlet.

### Kabul ölçütleri

- Paylaşılan URL yalnız aktif filtre ve gerekli sayfalama/sıralama parametrelerini taşır.
- Aynı içerik için gereksiz URL varyasyonları üretilmez.

## SF-007 — Çok görselli mobil galeri doğrulaması

### Yapılacaklar

- [x] En az üç sıralı görseli olan güvenli bir bileşen fixture'ı hazırla; gerçek API ürünü ayrıca bekleniyor.
- [ ] 390 px, geniş mobil, tablet ve desktop görünümünü doğrula.
- [ ] Scroll-snap, gösterge butonları, aktif görsel duyurusu ve reduced-motion davranışını test et.
- [x] Ana görselin preload/high-priority, diğerlerinin lazy olduğunu server markup testinde doğrula; browser network kontrolü bekleniyor.
- [x] Üç görselli fixture'da tüm görsellerin `4:5` geometrisini koruduğunu doğrula.

### Kabul ölçütleri

- Dokunma, kaydırma ve klavye/gösterge butonlarıyla tüm görsellere ulaşılabilir.
- Yatay sayfa taşması ve CLS oluşmaz.
- Desktop ana görsel ve alt görseller aynı galeri ekseninde hizalıdır.

## SF-008 — Kalıcı regresyon ve erişilebilirlik testleri

### Yapılacaklar

- [ ] Paket kurulumu öncesinde mevcut onayı ve en küçük Playwright/axe kapsamını yeniden doğrula.
- [ ] İzole guest cart ile ürün ekleme, ikinci ürün ekleme, adet güncelleme, kaldırma ve temizleme testlerini ekle.
- [ ] Checkout hydration, form validation, disabled order creation ve boş sepet testlerini ekle.
- [ ] Mobil menü Escape/focus restore ve filtre toggle klavye testlerini kalıcılaştır.
- [ ] Olmayan ürün HTTP durumu, canonical, robots ve JSON-LD testlerini ekle.
- [ ] Gerçek screen reader testini ayrıca yap; otomatik accessibility tree/axe sonucunu screen-reader doğrulaması sayma.

## SF-009 — Canlı içerik ve operasyon verisi

- [ ] Aktif kargo yöntemi adlarını ve ücretlerini canlıya uygun içerikle güncelle.
- [ ] Sıfır ürünlü marka/koleksiyon/tür seçeneklerini API-001 facet sözleşmesi tamamlanınca varsayılan filtreden çıkar.
- [ ] Ürünlerde eksik görsel, üçüncü taraf hotlink, alt metin, uzun isim ve tükenmiş stok durumlarını gözden geçir.
- [ ] Production görsel host/CDN ve kullanım hakkı kararını netleştir.

## SF-010 — Yayın öncesi SEO ve performans doğrulaması

- [ ] `STOREFRONT_APP_ORIGIN` değerini gerçek HTTPS production origin'i yap.
- [ ] Canonical, Open Graph, robots ve sitemap URL'lerini deployment üzerinde yeniden doğrula.
- [ ] Google Rich Results Test ile gerçek ürün URL'sini kontrol et.
- [ ] Search Console kaydı ve sitemap gönderimini tamamla.
- [ ] Catalog, product, cart ve checkout için production build üzerinde en az üç eş koşullu lab ölçümü al.
- [ ] CrUX veya gerçek RUM verisi oluştuğunda mobil/desktop 75. yüzdelik LCP, INP ve CLS'yi ayrı değerlendir.
- [ ] Gerçek cihaz, 200% zoom, 400% reflow ve screen reader kontrollerini tamamla.

Saha verisi oluşana kadar Core Web Vitals sonucu `not verified` olarak kalır.

## Uygulama sırası

1. `SF-001` checkout hydration mismatch.
2. `SF-002` gerçek 404 ve not-found deneyimi.
3. API ekibiyle paralel `SF-003` varyant snapshot sözleşmesi.
4. `SF-004` production sipariş oluşturma kapısı.
5. `SF-005` cookie proxy sertleştirmesi.
6. `SF-006` temiz filtre URL'leri.
7. `SF-007` çok görselli galeri fixture/testi.
8. `SF-008` kalıcı E2E ve erişilebilirlik regresyonları.
9. `SF-009` içerik/operasyon temizliği.
10. Deployment hazır olduğunda `SF-010` production SEO ve performans doğrulaması.

## Mevcut doğrulama tabanı

- [x] Varyantlı ve varyantsız üründe sepete ekleme çalıştı.
- [x] İkinci ürün ekleme ve adet artırma çalıştı.
- [x] Sepeti temizleme ve boş sepet/checkout durumları çalıştı.
- [x] Marka, koleksiyon ve ürün türü filtreleri çalıştı.
- [x] 390 px mobil genişlikte katalog, ürün, sepet ve checkout yatay taşma üretmedi.
- [x] Mobil menü Escape ile kapanıp odağı tetikleyiciye döndürdü.
- [x] Ürün canonical, Open Graph ve server HTML içindeki JSON-LD doğrulandı.
- [x] Origin'siz cart/checkout mutation `403`, geçersiz istek `400` döndürdü.
- [x] 55 Vitest testi geçti.
- [x] TypeScript, ESLint, OpenAPI tip kontrolü ve production build geçti.
- [ ] Çok görselli gerçek ürün akışı doğrulanmadı.
- [ ] Gerçek screen reader doğrulaması yapılmadı.
- [ ] Production domain, Search Console, CrUX/RUM ve saha CWV doğrulanmadı.

## Her işten sonra zorunlu doğrulama

Storefront kökünde:

```powershell
pnpm lint
pnpm typecheck
pnpm test
pnpm api:types:check
pnpm build
```

Değişen rota için ayrıca production build üzerinde desktop ve mobil runtime, console/network, keyboard/focus ve SEO header/HTML kontrolleri tekrarlanır.
