# Controller Kapsam Denetimi ve Tam Endpoint Envanteri

## 21 Ağustos 2026 iletişim mesajları güncellemesi

`ContactMessagesController` 7 operasyon ekler: public `POST /api/contact-messages`; AdminOnly liste, detay, status, assignment, note ve reply operasyonları. Public POST `security: []`, yaklaşık 16 KB body, idempotency, contact rate-limit ve production Turnstile koruması taşır. Admin operasyonları Bearer + AdminOnly'dir. `GET /api/users` response tipi `PagedResult<AdminUserDto>` olarak controller metadata'sında açıkça tanımlanmıştır. Güncel çalışan Swagger sözleşmesi toplam **280 operasyon** yayınlar.

## 16 Ağustos 2026 public kategori vitrini güncellemesi

16 Ağustos sürümünün runtime Swagger sözleşmesi tarihsel olarak **272 endpoint** yayınlıyordu. Anonim `GET /api/product-types/published` eklendi; ProductType envanteri 7'den 8 operasyona çıktı. ProductType kalıcı nullable `imageUrl` taşır; public vitrin özel görsel yoksa görünür yayımlanmış ürünler içindeki `PopularityScore DESC, Product.Id ASC` sırasının ilk ürün görselini toplu ve N+1 oluşturmadan döndürür. Aynı popularity fallback semantiği koleksiyon vitriniyle hizalandı.

## 16 Ağustos 2026 iyzico CheckoutForm sandbox güncellemesi

Güncel runtime Swagger sözleşmesi **271 endpoint** yayınlar. Üye ve guest initialize, anonim callback ve V3 imzalı webhook olmak üzere dört iyzico CheckoutForm endpointi eklendi. Initialize uçları kart verisi almaz; callback ve webhook OpenAPI'de `security: []` taşır. Payment yalnız imzalı retrieve yanıtı yerel order kimliği, TRY ve tutarlarla eşleştiğinde Paid olur.

## 14 Ağustos 2026 ortak guest session ve favoriler güncellemesi

Güncel runtime Swagger sözleşmesi **267 endpoint** yayınlar. Favori GET/POST/DELETE uçları JWT yanında ortak guest session sahipliğini destekler; yeni authenticated `POST /api/guest-session/claim` cart ve favorites verisini atomik claim eder. Anonymous favori operasyonları OpenAPI'de `security: []`, claim ise Bearer security taşır.

## 13 Ağustos 2026 public koleksiyon vitrini güncellemesi

Güncel runtime Swagger sözleşmesi **265 endpoint** yayınlar. Önceki 264 operasyona anonim `GET /api/collections/published` eklenmiştir. Collections envanteri 8'den 9 operasyona çıkmıştır. Endpoint OpenAPI'de `security: []` taşır ve sayfalı `PublishedCollectionShowcaseItemDto` kartlarını yayımlanmış ürün adedi ile etkili vitrin görseli üzerinden toplu döndürür.

## 13 Ağustos 2026 StoreSettings güncellemesi

Güncel runtime Swagger sözleşmesi **264 endpoint** yayınlar. Önceki 257 operasyona tek `StoreSettingsController` altında anonim public GET, AdminOnly admin GET ve beş AdminOnly section PUT olmak üzere 7 typed endpoint eklenmiştir. Public GET OpenAPI'de `security: []`; admin ve PUT uçları Bearer/AdminOnly sözleşmesindedir. Ayrıntılar `13-magaza-ayarlari/STORE-SETTINGS-SOZLESMESI.md` belgesindedir.

## 12 Ağustos 2026 yayımlanmış ürün facet güncellemesi

Güncel runtime Swagger sözleşmesi **257 endpoint** yayınlar. Önceki 254 operasyona `GET /api/products/published/facets/brands`, `/collections` ve `/product-types` olmak üzere 3 public facet endpointi eklenmiştir. Products envanteri 19'dan 22 operasyona çıkmıştır.

## 3 Ağustos 2026 guest checkout güncellemesi

3 Ağustos sürümündeki runtime Swagger keşfi **254 endpoint** buluyordu. Önceki sayımlara eklenen guest kapsamı 11 endpointtir:

| Route | Yetki | Başarı/bağlam |
| --- | --- | --- |
| `POST /api/cart/checkout/guest` | Public + guest protection | 201/200 OrderDto; zorunlu kargo/cart token/Idempotency-Key |
| `POST /api/guest-orders/access-links` | Public + Origin | Uniform 202; magic-link outbox |
| `POST /api/guest-orders/access/exchange` | Public + Origin | Tek kullanımlık link → guest session |
| `GET /api/guest-orders`, `GET /api/guest-orders/{id}` | Guest session | Liste/detail; cross-order 404 |
| `POST /api/guest-orders/{id}/payments`, `/cancel` | Guest session + CSRF + Origin | Mevcut payment/cancel kuralları |
| `GET/POST /api/guest-orders/{id}/returns`, `GET .../{returnId}` | Guest session | İade liste/create/detail |
| `POST /api/guest-orders/claim` | JWT + verified guest session + CSRF + Origin | Aynı e-postadaki sahipsiz siparişleri bağlar |

Bu bölümde aşağıda görünen eski 206/40/257/264/265/267/271/272 sayıları tarihsel envanterdir; güncel OpenAPI ve controller audit için 280 sayısı kullanılır.

Bu denetim 29 Temmuz 2026'da `API/src/ECommerce.API/Controllers` altındaki 33 controller doğrudan okunarak yapıldı. Route, HTTP fiili ve yetki için controller attribute'ları kaynak kabul edilmiştir.

## Sonuç

- Güncel runtime Swagger sözleşmesinde **280 endpoint** bulunuyor; aşağıdaki alan tabloları yetki ve davranış denetimini özetler.
- Eski fonksiyonel belgeler alanları anlatıyor; fakat her endpoint için ayrı request şeması ve başarılı JSON response örneği standardı yok. En büyük eksik muhasebe raporlarıdır: 28 route tek paragrafta özetlenmişti.
- Bu belge route kaçırılmasını önleyen zorunlu kontrol listesidir. `Public`: token yok; `User`: JWT ve sahiplik; `Admin`: JWT + `AdminOnly`/Admin rolü. Sayfalı yanıt `PagedResult<T>`dir.

## Auth, kullanıcı ve adres (23)

| Route | Yetki | Başarı | Amaç |
| --- | --- | --- | --- |
| `POST /api/auth/register` | Public | 201 `RegisterUserResultDto` | Hesap açar. |
| `POST /api/auth/login` | Public | `AuthResultDto` | Access/refresh token oturumu açar. |
| `POST /api/auth/refresh-token` | Public | `AuthResultDto` | Refresh token döndürür/rotate eder. |
| `POST /api/auth/logout` | Public | 204 | Refresh token oturumunu kapatır. |
| `POST /api/auth/forgot-password` | Public | 202 | E-posta varlığını ifşa etmeden reset akışını başlatır. |
| `POST /api/auth/reset-password` | Public | 204 | Reset token ile parolayı değiştirir. |
| `GET /api/users/me` | User | `UserDto` | Aktif kullanıcı profili. |
| `PUT /api/users/me/profile`, `/me/email` | User | `UserDto` | Profil veya e-posta güncellemesi. |
| `PUT /api/users/me/password`, `DELETE /me` | User | 204 | Parola değişikliği veya hesabı kapatma. |
| `GET /api/users/me/sessions` | User | `UserSessionDto[]` | Aktif oturumlar. |
| `DELETE /api/users/me/sessions/{sessionId}`, `/me/sessions` | User | 204 | Tek/tüm oturumları sonlandırır. |
| `GET /api/users` | Admin | `PagedResult<AdminUserDto>` | Filtreli kullanıcı listesi. |
| `GET /api/users/{publicUserId}` | Admin | `UserDto` | `U00001` ile kullanıcı detayı. |
| `PATCH /api/users/{publicUserId}/role`, `/{publicUserId}/status` | Admin | `AdminUserDto` | Rol veya hesap durumu değişimi. |
| `GET /api/addresses?type=…` | User | `AddressDto[]` | Kendi adresleri. |
| `POST /api/addresses` | User | 201 `AddressDto` | Adres oluşturur. |
| `PUT /api/addresses/{id}`, `PATCH /{id}/default` | User | `AddressDto` | Adresi günceller/varsayılan yapar. |
| `DELETE /api/addresses/{id}` | User | 204 | Sahibi olunan adresi siler. |

`LoginRequest={email,password,deviceName?}`, `RefreshTokenRequest={refreshToken,deviceName?}`, `LogoutRequest={refreshToken}`. User ID raw sayısal değil, `U` prefixli public ID'dir.

## Katalog

| Kaynak | Yetki ve gerçek endpointler | Mantık / başarı |
| --- | --- | --- |
| Products (22) | Public: `GET /published`, `/published/facets/brands`, `/published/facets/collections`, `/published/facets/product-types`, `/by-url/{url}`, `/seo-index`, `/by-collection/{collectionId}`, `/by-tag/{tagId}`, `/by-type/{typeId}`, `/by-brand/{brandId}`. Admin: `GET /api/products`, `GET /{productId}`, `POST /`, `POST /bulk`, `DELETE /{productId}`, `PUT /performance-metrics`, `PUT /{productId}`, `PATCH /{productId}/status`, `/activation`, `/featured`, `/has-variants`, `PUT /{productId}/relations`. | Delete 204 ile idempotent soft-delete uygular; operasyon geçmişi korunur ve ürün katalog okumalarından gizlenir. Admin ve storefront listeleri tür, marka, koleksiyon ve etiket filtrelerini destekler. Üç facet endpointi seçenekleri yayımlanmış ürün adetleriyle ve kendi boyut filtresini dışlayarak döndürür. `P…` public product ID kullanılır. |
| Product variants (8) | Public: `GET /api/product-variants/{id}`, `GET /by-product/{productId}`. Admin: `POST /by-product/{productId}`, `PUT /{id}`, `PATCH /{id}/price`, `POST /{id}/stock-movements`, `PATCH /{id}/activation`, `DELETE /{id}`. | Detay/mutasyon `ProductVariantDto`, create 201, delete 204. Stok doğrudan set edilmez; hareket yazılır. |
| Images (5) | Public: `GET /api/product-images/{id}`, `GET /by-product/{productId}`. Admin: `POST /by-product/{productId}`, `PUT /{id}`, `DELETE /{id}`. | `ProductImageDto`/liste, create 201, delete 204. |
| Brands (7) | Public: `GET /api/brands`, `GET /{id}`. Admin: `POST /`, `POST /bulk`, `PUT /{id}`, `PATCH /{id}/activation`, `DELETE /{id}`. | Delete 204; bağlı ürün korunur ve `brandId=null` olur. |
| Collections (9) | Public: `GET /api/collections`, `GET /published`, `GET /{id}`. Admin: `POST /`, `POST /bulk`, `PUT /{id}`, `PATCH /{id}/activation`, `PATCH /{id}/featured`, `DELETE /{id}`. | `/published` yalnız aktif ve yayımlanmış ürünü bulunan koleksiyonları adet ve etkili görselle toplu döndürür. Delete 204; ürün korunur, yalnız koleksiyon bağlantısı kaldırılır. |
| MainBanners (3) | Public: `GET /api/main-banners`. Admin: `GET /admin`, `PUT /api/main-banners`. | En fazla 5 resim/video; tek aktif main seçimi ilk sıraya normalize edilir. |
| AltBanner1–5 (15) | Her bölümde Public `GET /api/alt-banner-{1..5}`. Admin: ilgili `/admin` GET ve kök PUT. | Beş bağımsız bölümün her biri en fazla 5 resim/video taşır; bölüm güncellemeleri birbirini etkilemez. |
| Tags (7) | Public: `GET /api/tags`, `GET /{id}`. Admin: `POST /`, `POST /bulk`, `PUT /{id}`, `PATCH /{id}/activation`, `DELETE /{id}`. | Delete 204; ürün korunur, yalnız etiket bağlantısı kaldırılır. |
| Product types (8) | Public: `GET /api/product-types`, `GET /published`, `GET /{id}`. Admin: `POST /`, `POST /bulk`, `PUT /{id}`, `PATCH /{id}/activation`, `DELETE /{id}`. | `/published`, özel kategori görselini veya en popüler görünür ürün fallback'ini adetle toplu döndürür. Delete 204; bağlı ürün korunur ve `typeId=null` olur. |
| Engagement (9) | Public/User: `GET /api/product-engagement/favorites`, `POST/DELETE /products/{productId}/favorites`. User: `PUT /products/{productId}/rating`, `POST /products/{productId}/reviews`, `POST /products/{productId}/activities`. Public: `GET /products/{productId}/reviews`. Admin: `PATCH /reviews/{reviewId}/approval`, `GET /products/{productId}/metrics`. | JWT favorileri kullanıcıya, anonim favoriler ortak guest session'a aittir. Guest mutation Origin+CSRF ister. Favori/rating/activity çoğunlukla 204; inceleme 201; reviews sayfalıdır. |
| Stock movements (3) | Admin: `POST /api/stock-movements/bulk`, `GET /`, `GET /variants/{productVariantId}/balance`. | Atomik bulk hareket, defter `PagedResult<StockMovementDto>`, bakiye `StockBalanceDto`. |

Brand/Collection/Tag/ProductType create 201, detay/mutasyon ilgili DTO, liste `PagedResult<…Dto>` döner. Bu dört grupta route içindeki `/{id}` kendi kök yoluna göredir.

## Sepet, sipariş, iade ve yönetim (44)

| Route | Yetki | Başarı ve bağlam |
| --- | --- | --- |
| `GET /api/cart` | Public | `CartDto`; user veya HttpOnly guest-cookie sepeti. |
| `POST /api/cart/items` | Public | `CartDto`; `{productVariantId,quantity,expectedConcurrencyToken?}`. |
| `PUT /api/cart/items/{cartItemId}` | Public | `CartDto`; body'de quantity + expected token. |
| `DELETE /api/cart/items/{cartItemId}?expectedConcurrencyToken=…`, `DELETE /api/cart?expectedConcurrencyToken=…` | Public | `CartDto`; satırı/sepeti concurrency korumasıyla temizler. |
| `POST /api/guest-session/claim` | User | `GuestSessionClaimDto`; guest sepet ve favorilerini tek transaction ile öncelik kuralına göre claim eder. |
| `POST /api/cart/merge-guest` | User | Geriye uyumlu atomik claim; yalnız `CartDto` döndürür ve başarılıysa cookie silinir. |
| `POST /api/orders` | User | 201 `OrderDto`; cart concurrency token, opsiyonel adres/kupon/kargo ile checkout. |
| `GET /api/orders/mine`, `GET /api/orders/{id}` | User | Kendi sayfalı özetleri / `OrderDto`. |
| `POST /api/orders/{id}/payments` | User | 201 `PaymentDto`; `{provider}` ve zorunlu `Idempotency-Key`. |
| `POST /api/orders/{id}/payments/iyzico/checkout-form` | User | 201 `CheckoutFormSessionDto`; kart verisi almadan iyzico hosted form başlatır. |
| `POST /api/guest-orders/{id}/payments/iyzico/checkout-form` | Guest session | 201 `CheckoutFormSessionDto`; Origin, CSRF ve idempotency korumalıdır. |
| `POST /api/payments/iyzico/callback` | Public | İmzalı retrieve sonrası Storefront'a 303 yönlendirme. |
| `POST /api/payments/iyzico/webhook` | Public | `X-IYZ-SIGNATURE-V3` + retrieve sonrası idempotent 204. |
| `POST /api/orders/{id}/cancel` | User | `OrderDto`; ödeme öncesi iptal. |
| `GET /api/orders`, `GET /api/orders/admin/{id}`, `PATCH /api/orders/{id}/status` | Admin | Tüm siparişler, detay, yaşam döngüsü güncellemesi. |
| `POST /api/orders/reservations/expire` | Admin | `StockReservationExpirationResult`; süresi dolmuş rezervasyonları manuel tarar. |
| `POST /api/returns`, `GET /mine`, `GET /{id}` | User | 201 `ReturnRequestDto`, kendi liste/detayı. |
| `GET /api/returns`, `GET /admin/{id}`, `POST /{id}/approve`, `/reject`, `/receive`, `/complete` | Admin | İade operasyon listesi, detay ve durum geçişleri; karar body'si `{decisionNote?}`. |
| `GET /api/shipping-methods/active` | Public | `PagedResult<ShippingMethodDto>`; checkout seçimi. |
| `GET /api/shipping-methods`, `GET /{id}`, `POST /`, `PUT /{id}`, `PATCH /{id}/activation` | Admin | Kargo yönetimi. |
| `GET /api/tax-rates/active` | Public | `PagedResult<TaxRateDto>`; aktif oranlar. |
| `GET /api/tax-rates`, `GET /{id}`, `POST /`, `PUT /{id}`, `PATCH /{id}/activation` | Admin | Vergi yönetimi. |
| `GET /api/coupons`, `POST /`, `PUT /{id}`, `PATCH /{id}/activation` | Admin | `CouponDto`/sayfalı kupon yönetimi. |

Cart 60/dakika; order/return 30/dakika; order payment 10/dakika rate-limitlidir. Cart token uyumsuzluğu 409'dur; körlemesine retry yapılmamalıdır.

## Muhasebe (84, tamamı Admin)

Muhasebe e-commerce order/cart akışından bağımsızdır ve GUID kullanır. Draft belge stok/cari etkilemez; `post` operasyonları stok/maliyet/cari sonucu yaratabilir.

| Kaynak | Gerçek endpointler |
| --- | --- |
| Current accounts (4) | `POST /api/accounting/current-accounts`, `PUT /{id}`, `GET /{id}`, `GET /` |
| Accounting sales orders (9) | `POST /api/accounting/sales-orders`, `PUT /{id}`, `POST /{id}/items`, `PUT/DELETE /{id}/items/{itemId}`, `POST /{id}/post`, `POST /{id}/cancel`, `GET /{id}`, `GET /` |
| Sales invoices (10) | `POST /api/accounting/sales-invoices`, `POST /from-order/{accountingSalesOrderId}`, `PUT /{id}`, `POST /{id}/lines`, `PUT/DELETE /{id}/lines/{lineId}`, `POST /{id}/post`, `POST /{id}/cancel`, `GET /{id}`, `GET /` |
| Purchase invoices (13) | `POST /api/accounting/purchase-invoices`, `PUT /{id}`, `POST /{id}/lines`, `PUT/DELETE /{id}/lines/{lineId}`, `PUT /{id}/lines/{lineId}/allocations`, `POST /{id}/post`, `POST /{id}/cancel`, `POST/GET /{id}/expenses`, `GET /{id}`, `GET /`, `GET /available-stock-movements` |
| Payments (4) | `POST /api/accounting/payments`, `GET /{id}`, `POST /{id}/cancel`, `GET /` |
| Cash accounts (3) | `POST /api/accounting/cash-accounts`, `GET /`, `GET /{id}/statement` |
| Bank accounts (3) | `POST /api/accounting/bank-accounts`, `GET /`, `GET /{id}/statement` |
| Financial transactions (3) | `POST /api/accounting/financial-transactions`, `POST /bank-transfers`, `POST /{id}/reverse` |
| Expenses (4) | `POST/GET /api/accounting/expenses/categories`, `POST/GET /api/accounting/expenses` |
| Cost history (3) | `GET /api/accounting/inventory-cost-layers/opening-balance/by-variant/{productVariantId}`, `PATCH /{id}/opening-balance-cost`, `GET /api/accounting/product-variants/{productVariantId}/cost-history` |

### Muhasebe raporları: 28 ayrı GET route

Her biri `PagedResult<AccountingReportRowDto>` döner ve `ReportFilter` query'sini alır: `GET /api/accounting/reports/sales`, `/sales/{id}`, `/sales/{id}/items`, `/sales-invoices`, `/sales-invoices/{id}`, `/purchase-invoices`, `/purchase-invoices/{id}`, `/stock-movements/uncosted`, `/stock-movements/partially-costed`, `/cost-layers`, `/cost-layers/remaining`, `/cost-layer-consumptions`, `/product-variant-cost-history`, `/warehouse-stock-valuation`, `/profitability/products`, `/profitability/product-variants`, `/profitability/sales-orders`, `/profitability/sales-invoices`, `/current-accounts/{id}/statement`, `/receivables`, `/debts`, `/overdue-receivables`, `/overdue-debts`, `/payments-and-collections`, `/cash-movements`, `/bank-movements`, `/vat/purchases`, `/vat/sales`.

`uncosted` maliyet katmanı bekleyen çıkışlar; `partially-costed` kısmen maliyetlenmişler; `warehouse-stock-valuation` stok değeridir. Kârlılık endpointleri ürün, varyant, satış siparişi ve fatura kırılımındadır. Cari statement tek cari ekstre, receivable/debt/overdue listeleri tahsilat operasyon ekranlarıdır.

## Belirlenen dokümantasyon eksikleri

1. Muhasebe için 84 endpoint karşılığında yalnız 5 JSON örneği vardı. Financial transaction, bank transfer, kasa/banka, expense, cost-layer patch ve rapor query/row örnekleri eksiktir.
2. Katalogta 59 endpoint için 3 JSON bloğu vardı. Relations, varyant fiyat/stok, image, engagement ve bulk yönetimin somut response'ları yoktur.
3. Auth/user için register response ve profile/email/password/close-account body sözleşmeleri; admin list/session örnekleri eksiktir.
4. Reservation-expire route'u eski navigasyonda görünmüyordu. Adres, vergi/kargo, kupon ve iade kararlarının somut response örnekleri de tamamlanmalıdır.

Her mutasyon dokümanında body alanları/nullable/enum, bir 2xx JSON örneği, 400/401/403/404/409 davranışı, state yenileme etkisi ve idempotency/concurrency kuralı bulunmalıdır. Ortak hata biçimi için [genel kurallar](00-genel/01-api-kurallari.md), fonksiyonel ayrıntı için ilgili bölümler kullanılmalıdır.
