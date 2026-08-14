# Storefront MVP SEO ve Performans Sertleştirme Raporu

**Rapor tarihi:** 11 Ağustos 2026  
**Son UI sertleştirme doğrulaması:** 12 Ağustos 2026  
**Uygulama:** `UI/storefront`  
**Framework:** Next.js 16.2.12, React 19.2.4, App Router  
**Build durumu:** Değişiklikler henüz commit edilmediği için build SHA bulunmuyor. Production build başarılıdır.

## Yönetici özeti

Storefront MVP'nin ana sayfa, ürün kataloğu ve ürün detay rotaları production build üzerinde SEO ve performans açısından incelendi. Gerçek LCP görsellerinin yanlış yükleme önceliği, katalogdaki gereksiz RSC prefetch trafiği, eski ürün URL'sinin meta refresh ile yönlenmesi, sıralanmış katalog canonical'ının sayfa numarasını kaybetmesi ve boş ileri katalog sayfalarının indexlenebilir olması düzeltildi.

Son durumda:

- Üç temsilî public rotanın tamamında ölçülen CLS `0.00` oldu.
- Üç rotada da LCP kaynak keşfi uyarısı ortadan kalktı.
- Katalog açılışındaki toplam ağ isteği aynı inceleme akışında `47` isteğin `19` isteğe düşmesiyle yaklaşık `%60` azaldı.
- Eski `/product/[slug]` rotası gerçek `308 Permanent Redirect` döndürüyor.
- Ürün filtresi ve alternatif sıralama sayfaları `noindex, follow`; sepet ve checkout rotaları `noindex, nofollow` olarak doğrulandı.
- Ürün detayında initial server HTML içinde `Product` ve `BreadcrumbList` JSON-LD bulunuyor.
- Mobil Lighthouse incelemesinde ürün detay rotası SEO, Accessibility ve Best Practices kategorilerinde `100` aldı.

Bu sonuçlar yerel laboratuvar kanıtıdır. CrUX, Search Console veya gerçek RUM verisi bulunmadığı için Core Web Vitals saha başarısı doğrulanmış sayılmaz.

## Ölçüm ortamı

| Alan | Değer |
| --- | --- |
| Sunum biçimi | Yerel production build, `next start` |
| Test origin'i | `http://localhost:3100` |
| API | Yerel API, `http://localhost:3300` |
| Profil | Mobil Chromium emülasyonu |
| Viewport | `390 × 844`, DPR 3 |
| Ağ | Slow 4G emülasyonu |
| CPU | 4× slowdown |
| Kullanıcı durumu | Anonymous/guest |
| Veri durumu | Yayımlanmış ürün ve banner verisi |
| Son ölçüm örneği | Her public rota için 3 trace |
| Cache durumu | Aynı browser context içinde warm/mixed cache |
| Saha verisi | Mevcut değil — not verified |

Lighthouse performans kategorisi kullanılmadı. Performans ölçümleri Chrome DevTools Performance Trace üzerinden alındı; Lighthouse yalnız SEO, erişilebilirlik ve best-practices teşhisi için kullanıldı.

## 10 üzerinden puanlama yöntemi

Rota performans puanı aşağıdaki ağırlıklarla hesaplandı:

| Ölçüt | En yüksek puan |
| --- | ---: |
| Yerel lab LCP sonucu | 3.0 |
| CLS sonucu ve ayrılmış medya alanı | 2.0 |
| LCP kaynak keşfi ve yükleme önceliği | 2.0 |
| Ağ isteği ve client/prefetch verimliliği | 1.5 |
| Console ve network runtime kararlılığı | 0.5 |
| CrUX/Search Console/RUM ile saha CWV ve INP kanıtı | 1.0 |
| **Toplam** | **10.0** |

Saha verisi bulunmadığı için son ölçütte tüm rotalar `0/1` aldı. Bu puanlar Google sıralama puanı, Lighthouse performans skoru veya Core Web Vitals geçiş garantisi değildir; yalnız bu rapordaki kanıtların proje içi değerlendirmesidir.

## Rota bazlı performans sonuçları

| Rota | Trace örnekleri | Medyan LCP | CLS | LCP keşif uyarısı | Performans puanı |
| --- | --- | ---: | ---: | --- | ---: |
| Ana sayfa `/` | 971 / 905 / 871 ms | **905 ms** | **0.00** | Yok | **8.7/10** |
| Katalog `/products` | 1089 / 1189 / 814 ms | **1089 ms** | **0.00** | Yok | **8.8/10** |
| Ürün `/products/[slug]` | 1055 / 922 / 972 ms | **972 ms** | **0.00** | Yok | **8.8/10** |
| **Public rota ortalaması** | — | — | — | — | **8.8/10** |

### Ana sayfa analizi — 8.7/10

**LCP adayı:** Ana banner görseli.  
**Doğrulanan durum:** Görsel initial HTML içinde bulunuyor, `loading="eager"`, `fetchPriority="high"`, doğru responsive `sizes` ve sabit oranlı container kullanıyor. Son üç trace'in medyan LCP değeri `905 ms`, CLS değeri `0.00` oldu.

**Puan kırılan alanlar:** Ana sayfa için gerçek kullanıcı INP/CWV verisi yok. Aynı browser context kullanıldığı için ölçüm tam cold-cache saha davranışını temsil etmiyor.

### Katalog analizi — 8.8/10

**LCP adayı:** İlk ürün kartının ana görseli.  
**Doğrulanan durum:** Yalnız ilk kart eager/high-priority yükleniyor; diğer kartlar lazy kalıyor. Ürün kartları, sıralama seçenekleri ve tekrar eden shell linkleri için otomatik prefetch kapatıldı. Katalog açılışındaki ağ isteği `47 → 19` oldu ve gereksiz ürün/sıralama RSC prefetch çağrıları kayboldu. Son üç trace'in medyan LCP değeri `1089 ms`, CLS değeri `0.00` oldu.

**Puan kırılan alanlar:** Gerçek kullanıcı filtre, sıralama ve kart tıklama INP verisi yok. API facet sözleşmesinde ürün adetleri henüz bulunmadığı için filtre verimliliğinin backend tarafındaki nihai hali tamamlanmadı.

### Ürün detay analizi — 8.8/10

**LCP adayı:** Ürün galerisinin ana görseli.  
**Doğrulanan durum:** Ana görsel eager/high-priority, diğer galeri görselleri lazy yükleniyor; tüm medya `4:5` oranlı sabit alanda render ediliyor. Son üç trace'in medyan LCP değeri `972 ms`, CLS değeri `0.00` oldu. Ürün metadata'sı, canonical, Open Graph, Twitter, Product JSON-LD ve BreadcrumbList aynı otoriter ürün verisinden oluşturuluyor.

**Puan kırılan alanlar:** Varyant seçimi ve sepete ekleme etkileşimleri için gerçek kullanıcı INP verisi yok. Google Rich Results Test deployment üzerindeki gerçek HTTPS URL ile çalıştırılmadı.

## Teknik SEO analizi — 9.0/10

| Alan | Sonuç | Puan |
| --- | --- | ---: |
| Canonical ve query politikası | Temiz katalog self-canonical; sıralama canonical'ı sayfayı koruyor; filtreler noindex | 2.0/2.0 |
| Ürün metadata ve structured data | Dynamic metadata, Product ve BreadcrumbList initial HTML içinde | 2.0/2.0 |
| Redirect politikası | Eski tekil ürün yolu gerçek 308 ile canonical rotaya gidiyor | 1.5/1.5 |
| Sitemap ve robots | 6 canonical ürün URL'si var; cart/checkout/api yok; sitemap robots içinde | 1.5/1.5 |
| Private/işlem rotaları | Cart, checkout ve confirmation noindex/nofollow | 1.0/1.0 |
| Runtime SEO denetimi | Mobil Lighthouse SEO 100; rendered HTML ve header matrisi doğrulandı | 1.0/1.0 |
| Production origin ve dış doğrulama | Gerçek HTTPS domain, Search Console ve Rich Results Test henüz yok | 0.0/1.0 |
| **Toplam** |  | **9.0/10** |

## Indexlenebilirlik matrisi

| Rota ailesi | HTTP/render durumu | Robots | Canonical | Sitemap | Structured data |
| --- | --- | --- | --- | --- | --- |
| `/` | 200 | Indexlenebilir | Self | Evet | Yok |
| `/products` | 200 | `index, follow` | Self | Evet | Yok |
| `/products?page=N` geçerli | 200 | `index, follow` | Kendi sayfası | Hayır | Yok |
| `/products?sort=...` | 200 | `noindex, follow` | Aynı sayfanın varsayılan sırası | Hayır | Yok |
| `/products?brand/collection/type=...` | 200 | `noindex, follow` | Filtrelenmiş temiz URL | Hayır | Yok |
| Boş ileri katalog sayfası | Gerçek HTTP `404` | `noindex` | Yok | Hayır | Yok |
| `/products/[slug]` | 200 | Indexlenebilir | API slug'ıyla self | Evet | Product + BreadcrumbList |
| `/product/[slug]` | 308 | Uygulanmaz | `/products/[slug]` hedefi | Hayır | Yok |
| `/cart` | 200 | `noindex, nofollow` | Yok | Hayır | Yok |
| `/checkout` | 200 | `noindex, nofollow` | Yok | Hayır | Yok |
| `/checkout/confirmation/[orderId]` | 200 | `noindex, nofollow` | Yok | Hayır | Yok |
| `/api/**` | BFF/internal | robots.txt ile crawl dışı | Yok | Hayır | Yok |

12 Ağustos sertleştirmesinde root, katalog ve ürün seviyesindeki erken streaming loading sınırları kaldırıldı. Boş ileri katalog sayfası production runtime'da gerçek HTTP `404`, Türkçe not-found içeriği ve `noindex` ile doğrulandı. Olmayan ürün slug'ının HTTP durumu, yerel API bu kontrolde çalışmadığı için tekrar doğrulanamadı; API açıkken aynı kontrol yenilenmelidir.

## 12 Ağustos 2026 Takip Doğrulaması

- Hydration-safe cart/checkout başlangıcı, güvenli guest cookie allowlist'i, production checkout kilidi ve temiz filtre URL yönlendirmesi uygulandı.
- ESLint, TypeScript, OpenAPI tip kontrolü, 55 Vitest testi ve Next.js production build geçti.
- `/products?page=999` gerçek `404`; `brand=&collection=&type=` isteği `307 /products`; production checkout mutation'ı `503 checkout_unavailable` döndürdü.
- Bu oturumda tarayıcı bağlantısı bulunmadığından önceki LCP/CLS trace ölçümleri yeniden alınmadı. Bu nedenle rota puanları yeni ölçüm yapılmadan yükseltilmedi veya düşürülmedi; yukarıdaki **8.7–8.8/10** performans puanları 11 Ağustos lab koşusuna aittir.
- CrUX/RUM, gerçek domain, gerçek screen reader ve üç görselli gerçek API ürünü hâlâ `not verified` durumundadır.

## Düzeltilen sorunlar

### 1. LCP görsellerinin düşük önceliği

- **Önce:** Ana banner ve ürün ana görseli browser trace içinde düşük priority görünüyordu. Katalogdaki ilk LCP görseli lazy-load ediliyordu.
- **Sonra:** Gerçek LCP adayı `eager + fetchPriority=high`; diğer görseller lazy kaldı.
- **Etkilenen kod:**
  - `storefront/src/modules/banners/components/banner-sections.tsx`
  - `storefront/src/modules/catalog/components/product-card.tsx`
  - `storefront/src/modules/product/components/product-gallery.tsx`
- **Doğrulama:** Son dokuz trace'in hiçbirinde LCPDiscovery uyarısı oluşmadı.

### 2. Gereksiz RSC prefetch trafiği

- **Önce:** Katalog ilk açılışında görünür ürün, sıralama, cart ve tekrar eden navigasyon bağlantıları çok sayıda RSC prefetch isteği oluşturuyordu.
- **Sonra:** Ürün kartı, sıralama, filtre temizleme ve tekrar eden shell bağlantılarında otomatik prefetch kapatıldı.
- **Ölçülen sonuç:** `47 → 19` toplam istek; yaklaşık `%60` azalma.
- **Doğrulama:** Son katalog network envanterinde ürün/sıralama RSC prefetch isteği bulunmadı.

### 3. Meta refresh kullanan eski ürün rotası

- **Önce:** `/product/[slug]` cevabı `200 OK` ve HTML meta refresh üretiyordu.
- **Sonra:** `next.config.ts` redirect kuralı gerçek `308 Permanent Redirect` üretiyor; eski route dosyası kaldırıldı.

### 4. Hatalı katalog canonical sayfalaması

- **Önce:** Alternatif sıralamada `page=2` canonical'ı `/products` sayfa 1'e gidiyordu.
- **Sonra:** Sıralama parametresi kaldırılırken gerçek sayfa numarası ve filtre kapsamı korunuyor.
- **Regresyon testi:** `catalogCanonicalHref({ page: 3, sort: "popular" })` sonucu `/products?page=3`.

### 5. Indexlenebilir boş ileri sayfalar

- **Önce:** Ürün olmayan `/products?page=2` boş içerikle `index, follow` dönebiliyordu.
- **Sonra:** Metadata ve sayfa render sınırında not-found üretiliyor; rendered sonuç `noindex` taşıyor ve boş katalog içeriği gösterilmiyor.

### 6. Gereksiz framework response başlığı

- `X-Powered-By` header'ı `poweredByHeader: false` ile kaldırıldı.

## Static audit sonuçlarının değerlendirilmesi

SEO static audit sonrasında iki lead kaldı:

1. Checkout confirmation dynamic metadata lead'i gerçek defect değildir; rota yerel statik metadata ile `noindex, nofollow` tanımlar.
2. Products query indexability lead'i gerçek defect değildir; desteklenen query ailelerinin canonical ve robots davranışı runtime HTML ile ayrı ayrı doğrulandı.

Performance static audit lead'leri:

1. Turnstile raw script lead'i gerçek başlangıç maliyeti değildir; `next/script` yalnız API challenge istediğinde `afterInteractive` stratejisiyle render edilir.
2. `generated/api.ts` büyük bir generated type dosyasıdır; runtime value olarak browser bundle'a taşındığına dair kanıt yoktur.
3. Client Component kayıtları interaktif en küçük yapraklar olarak incelendi; katalog ve public içerik Server Component kalmaya devam ediyor.

## Test ve doğrulama özeti

| Kontrol | Sonuç |
| --- | --- |
| OpenAPI type drift | Başarılı |
| TypeScript | Başarılı |
| ESLint | Başarılı |
| Unit/component testleri | 14 dosya, 45 test başarılı |
| Production build | Başarılı |
| Route envanteri | Legacy route dosyası yok; canonical ürün rotası mevcut |
| Runtime canonical/robots/JSON-LD | Başarılı |
| Legacy redirect | 308 doğrulandı |
| Console hataları | Yok |
| Beklenmeyen network hataları | Yok |
| Mobil Lighthouse SEO | 100 |
| Mobil Lighthouse Accessibility | 100 |
| Mobil Lighthouse Best Practices | 100 |
| Gerçek screen reader | Not verified |
| CrUX/Search Console/RUM | Not verified |
| Gerçek mobil cihaz | Not verified |
| Production HTTPS domain | Not configured/not verified |

## Kalan riskler ve sonraki adımlar

### Yüksek öncelik

1. `STOREFRONT_APP_ORIGIN` canlıya çıkmadan önce gerçek HTTPS production origin'i olmalıdır. Mevcut yerel değer `http://localhost:3000` olduğu için canlı canonical, robots host ve sitemap URL'leri deployment ayarı yapılmadan doğru kabul edilemez.
2. Search Console ve mümkünse route-template bazlı RUM kurulmadan Core Web Vitals saha başarısı doğrulanmış sayılmamalıdır.
3. Gerçek production URL üzerinde Google Rich Results Test ile Product/Breadcrumb eligibility kontrol edilmelidir.

### Orta öncelik

1. Marka, koleksiyon ve ürün türü filtreleri API'den `productCount` içeren facet sözleşmesi aldığında sıfır sonuçlu seçenekler N+1 istek olmadan kaldırılmalıdır. Ayrıntı `UI/API-IMPROVEMENT-RECOMMENDATIONS.md` içindeki `API-001` kaydındadır.
2. Header menüsü, filtreler, varyant seçimi, sepet ve checkout etkileşimleri için gerçek kullanıcı INP/RUM verisi toplanmalıdır.
3. Gerçek cihaz ve gerçek screen reader ile mobil navigasyon, galeri, filtre ve satın alma akışı doğrulanmalıdır.

## Son değerlendirme

| Alan | Puan |
| --- | ---: |
| Public rota performansı | **8.8/10** |
| Teknik SEO | **9.0/10** |
| Ağ/prefetch verimliliği | **9.2/10** |
| Runtime kararlılığı | **9.5/10** |
| Production ve saha doğrulama hazırlığı | **5.0/10** |
| **Genel mevcut durum** | **8.7/10** |

Genel puanın production ve saha doğrulama puanından yüksek olması, uygulama kodu ve yerel production ölçümünün güçlü olmasından kaynaklanır. `10/10` için yalnız daha fazla kod değişikliği yeterli değildir; gerçek HTTPS deployment, Search Console/CrUX veya RUM verisi, gerçek cihaz ve erişilebilirlik doğrulaması gerekir.
