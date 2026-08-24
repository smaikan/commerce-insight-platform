# Guest Checkout Uygulama Raporu

## 1. Amaç ve değişmez kurallar

Bu geliştirme, ortak/default bir User oluşturmadan anonymous sepetin siparişe dönüşmesini, guest müşterinin siparişine güvenli biçimde erişmesini ve üyeye özel kupon ayrımını ekler. Guest siparişlerde `Order.UserId=null` kalır. Fiyat, vergi, indirim, kargo ücreti, stok ve toplam frontend girdisi değildir. Kargo yöntemi zorunludur ve adı/ücreti aktif backend kaydından snapshot edilir.

Stok yalnız mevcut `ProductVariant.ApplyStockMovement`/`StockMovement` ledger yoluyla değişir. Checkout negatif `Sale`, iptal/rezervasyon sonlandırma mevcut pozitif `Cancellation` hareketini kullanır. Guest için ikinci stok mekanizması eklenmemiştir.

## 2. Mimari gerekçe ve checkout sırası

Üye ve guest checkout fiyatlandırma veya stok kurallarını çoğaltmaz. `OrderCheckoutOrchestrator` şu güvenilir sırayı uygular:

1. Cart owner (JWT user veya guest cart session) ve beklenen concurrency token doğrulanır.
2. Cart kalemleri kilitli Product/ProductVariant sorgularıyla yeniden okunur; aktiflik, güncel fiyat ve stok kontrol edilir.
3. Müşteri ile shipping/billing kaynakları çözümlenir.
4. `shippingMethodId` backend'den kilitli okunur; bulunmayan/pasif yöntem siparişi engeller.
5. Kupon uygunluğu indirimden önce değerlendirilir. Guest + `IsMemberOnly=true` tam `409 coupon_members_only` üretir.
6. Net tutar, indirim, vergi, backend kargo ücreti ve grand total merkezi pricing servisiyle hesaplanır.
7. Order, OrderItem, customer/address/shipping snapshot'ları oluşturulur.
8. Ücret sıfır değilse 15 dakikalık stok rezervasyonu başlatılır; sıfır toplamlı sipariş Paid durumuna ilerler.
9. Kupon kullanımı `OrderId` ile yazılır; her varyant için tek negatif `Sale` StockMovement ve purchase metriği oluşturulur.
10. Cart temizlenir ve e-posta outbox kaydı hazırlanır.

Guest handler bu ortak akışı guest session, order grant, magic-link, idempotency ve korunmuş outbox tokenıyla aynı serializable transaction içinde tamamlar. SMTP gönderimi background worker'da gerçekleştiği için checkout HTTP cevabı e-posta teslimini beklemez.

## 3. Veri modeli

| Yapı | İşlev |
| --- | --- |
| `Order.UserId` | Üyede `long`, guest'te null; claim sonrası kullanıcıya bağlanır. |
| `OrderCustomerSnapshot` | Sipariş anındaki ad, soyad, normalize e-posta ve telefon. |
| `OrderAddressSnapshot` | Shipping ve Billing snapshot; guest için `SourceAddressId=null`. |
| `Coupon.IsMemberOnly` | Varsayılan false; true yalnız üyeye izin verir. |
| `CouponUsage.UserId` | Guest'te null, OrderId zorunlu kullanım bağı; claim ile atanabilir. |
| `ReturnRequest.UserId` | Guest iadesinde null, claim ile atanabilir. |
| `GuestOrderSession` | 7 günlük hash tabanlı session, CSRF hash'i ve doğrulanmış e-posta hash'i. |
| `GuestOrderAccessGrant` | Bir session'ın erişebildiği tek Order yetkisi; revoke/reactivate destekler. |
| `GuestOrderMagicLink` | 30 dakikalık, tek kullanımlı, hash tabanlı erişim tokenı. |
| `GuestCheckoutIdempotency` | Cart session + key hash, request hash ve Order sonucunu 24 saat bağlar. |

Migration mevcut kuponları `IsMemberOnly=false` ile korur; sahiplik foreign key'lerini nullable yapar; mevcut Order müşteri snapshot'larını User verisinden ve billing snapshot'larını shipping snapshot'tan backfill eder. Migration dosyası kalıcı geliştirme veritabanına otomatik uygulanmamıştır.

## 4. Dosya ve işlev haritası

### Domain ve persistence

- `src/ECommerce.Domain/Entities/Order/Order.cs`: nullable owner, customer/address snapshot ve claim.
- `src/ECommerce.Domain/Entities/Order/OrderCustomerSnapshot.cs`: değişmez müşteri snapshot'ı.
- `src/ECommerce.Domain/Entities/Order/OrderAddressSnapshot.cs`: shipping/billing ve nullable source address.
- `src/ECommerce.Domain/Entities/Guest/*`: session, access grant ve magic-link domain kuralları.
- `src/ECommerce.Domain/Entities/Guest/GuestCheckoutIdempotency.cs`: checkout replay kaydı.
- `src/ECommerce.Domain/Entities/Coupon/Coupon.cs`: `IsMemberOnly`.
- `src/ECommerce.Persistence/Configurations/*Guest*` ve `OrderCustomerSnapshotConfiguration.cs`: tablo, index, ilişki ve uzunluklar.
- `src/ECommerce.Persistence/Repositories/GuestOrderRepository.cs`: session/grant/order/return/claim/idempotency sorguları.
- `src/ECommerce.Persistence/Migrations/20260803000116_AddGuestCheckoutAndMemberOnlyCoupons.cs`: şema ve backfill.

### Application ve API

- `src/ECommerce.Application/Orders/Services/OrderCheckoutOrchestrator.cs`: guest ve üyede ortak güvenilir checkout.
- `src/ECommerce.Application/GuestOrders/Checkout/*`: guest command, validation, transaction ve replay.
- `src/ECommerce.Application/GuestOrders/GuestOrderAccessService.cs`: access-link, exchange, session doğrulama, liste/detail ve claim.
- `src/ECommerce.Application/GuestOrders/GuestOrderOperationsService.cs`: ödeme, provider kontrollü iptal, stok geri alma, kupon release ve iade kurallarının guest yüzeyi.
- `src/ECommerce.API/Controllers/Cart/CartController.cs`: `POST /api/cart/checkout/guest`, guest cart/order cookie yazımı ve protection çağrısı.
- `src/ECommerce.API/Controllers/GuestOrders/GuestOrdersController.cs`: magic-link ve self-service uçları, Origin/CSRF/cookie sınırı.
- `src/ECommerce.Application/Orders/Services/OrderCouponService.cs`: member-only ayrımı ve nullable guest kullanım kaydı.
- `src/ECommerce.API/ErrorHandling/*`: özel ProblemDetails status/type/code sözleşmesi.

### Güvenlik, e-posta ve yapılandırma

- `src/ECommerce.Infrastructure/Security/GuestTokenService.cs`: 32 byte CSPRNG token ve SHA-256 uppercase hex hash.
- `GuestOrderAccessTokenProtector.cs`: outbox tokenını ASP.NET Data Protection ile korur.
- `GuestCheckoutProtectionService.cs`: Redis sayaçları, process içi fallback ve Turnstile kararı.
- `TurnstileVerifier.cs`: Cloudflare server-side siteverify istemcisi.
- `EmailOutboxBackgroundService.cs` ve `SmtpEmailSender.cs`: korunmuş tokenı yalnız gönderim anında çözüp magic-link e-postasını gönderir; mevcut retry/dead-letter/lease davranışını kullanır.
- `appsettings.json`: `GuestProtection:TrustedOrigins`, Redis bağlantısı, Turnstile secret ve `Email:GuestOrderAccessUrl` yapılandırma anahtarları.

## 5. HTTP endpointleri

| Method | Route | Yetki |
| --- | --- | --- |
| POST | `/api/cart/checkout/guest` | Guest cart cookie + Origin + Idempotency-Key; gerektiğinde Turnstile |
| POST | `/api/guest-orders/access-links` | Public, uniform 202, Origin ve guest-only magic-link limiti |
| POST | `/api/guest-orders/access/exchange` | Tek kullanımlık token + Origin; session/grant üretir |
| GET | `/api/guest-orders` | Guest session grant, no-store |
| GET | `/api/guest-orders/{id}` | Guest session grant, no-store; yetkisiz order 404 |
| POST | `/api/guest-orders/{id}/payments` | Session + Origin + CSRF + Idempotency-Key |
| POST | `/api/guest-orders/{id}/cancel` | Session + Origin + CSRF |
| GET/POST | `/api/guest-orders/{id}/returns` | Session; POST ayrıca Origin + CSRF |
| GET | `/api/guest-orders/{id}/returns/{returnId}` | Session/order grant, no-store |
| POST | `/api/guest-orders/claim` | JWT + aynı doğrulanmış e-posta + Origin + CSRF |

Tam wire örnekleri ve frontend recovery davranışları `UI/docs/api/api-project-docs/08-endpoint-sozlesmeleri` altındadır.

## 6. Zorunlu kargo ve kupon

Guest request zorunlu `shippingMethodId` ve shipping snapshot alır. Üye request de zorunlu kayıtlı `shippingAddressId` ve `shippingMethodId` alır. Kargo checkout sırasında yeniden okunur; pasif veya silinmişse transaction Order oluşturmadan sonlanır.

`Coupon.IsMemberOnly=false` guest ve üyede diğer kupon koşullarına göre kullanılabilir. `true` guest'te hesaplamadan önce şu cevabı üretir:

```json
{"status":409,"type":"urn:ecommerce:error:coupon_members_only","code":"coupon_members_only","detail":"Bu kuponu kullanmak için üye hesabıyla giriş yapmalısınız."}
```

Guest kupon kullanımı User yerine Order üzerinden izlenir ve mevcut cancellation release akışıyla geri alınır.

Guest iptalinde bekleyen iyzico ödeme önce sahiplik + Origin + CSRF doğrulamasından sonra transaction dışında retrieve edilir. Paid sonuç iptali engeller; kesin failure Payment/Order, stok, kupon ve outbox'ı aynı transaction'da bir kez sonuçlandırır; Unknown/bağlantı hatası/`fraudStatus=0` rezervasyonu korur. İptalde guest cart temizlenmez.

## 7. Guest limitleri ve hata kodları

- 10 dakikadaki üçüncü checkout denemesinde Turnstile.
- IP başına 15 dakikada 5 checkout.
- Cart session + normalize e-posta kimliği başına saatte 5 checkout.
- Session veya e-posta başına en fazla 3 aktif ödenmemiş rezervasyon.
- Magic-link sipariş başına saatte 3, IP başına saatte 10.
- Redis yoksa process içi fallback kullanılır ve Turnstile zorunlu olur.

Özel kodlar: `guest_checkout_challenge_required` (428), `guest_checkout_rate_limited` (429), `guest_checkout_protection_unavailable` (503), `coupon_members_only` (409), `invalid_guest_access` (401/403/404 semantiğine göre) ve `idempotency_key_reused` (409).

## 8. Magic-link, aynı/farklı cihaz ve claim

Checkout aynı cihazda hemen 7 günlük session ve Order grant'i verir. Farklı cihazda kullanıcı sipariş numarası/e-postayla access-link ister; cevap eşleşme olsun veya olmasın uniform 202'dir. Token e-postadaki URL'nin fragment bölümündedir, 30 dakika geçerlidir ve exchange sırasında tek kullanımlık tüketilir. Sipariş numarası/e-posta doğrudan yetki sağlamaz.

Claim, JWT User e-postasının session'daki magic-link doğrulanmış e-posta hash'iyle eşleşmesini ister. Aynı normalize e-postadaki bütün sahipsiz Order kayıtları ile ReturnRequest/CouponUsage sahiplikleri tek transaction'da kullanıcıya bağlanır; grant, link ve session iptal edilir. Guest, claim öncesi UserId tabanlı review/rating uygunluğunu sağlayamaz.

## 9. Next.js BFF sözleşmesi

Browser guest işlemlerini same-origin Route Handler'a yapar. BFF yalnız allowlist edilmiş guest cookie/header'larını ASP.NET API'ye taşır; upstream `Set-Cookie` değerlerini storefront origin altında Secure/HttpOnly/SameSite=Lax olarak yeniden yazar. Cookie/token Client Component, localStorage, DOM, log veya analytics'e açılmaz.

BFF mutation öncesi Origin doğrular, CSRF cookie'sini server-side okuyup `X-Guest-CSRF` header'ına koyar. Magic-link tokenı URL fragment'ından exchange body alanına taşınır ve URL temizlenir. Server Component kendi Route Handler'ına HTTP çağrısı yapmaz; ortak server-only API fonksiyonunu kullanır. Guest cart/order/detail cevapları `no-store` olur.

## 10. Kurulum ve yapılandırma

Üretim ortamında aşağıdakiler secret/config provider üzerinden verilmelidir:

- `ConnectionStrings:Redis`
- `Turnstile:SecretKey`
- `GuestProtection:TrustedOrigins` (noktalı virgülle ayrılmış kesin origin allowlist)
- `Email:GuestOrderAccessUrl` (token içermeyen taban URL; örnek: `https://store.example.com/guest-orders/access`; sender `#token=...` ekler)
- mevcut SMTP ve Data Protection key-ring yapılandırması

Birden fazla instance'ta ortak Redis ve ortak kalıcı Data Protection key-ring zorunludur. Turnstile secret veya servis yokken Redis fallback challenge'ı doğrulanamaz ve güvenli biçimde 503 döner.

## 11. Migration ve doğrulama

Migration oluşturuldu fakat kalıcı development veritabanına uygulanmadı. Doğrulama adımları:

```powershell
dotnet build ECommerce.sln --no-restore
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build
dotnet ef migrations script 20260801000032_AddProductSeoAndUrlRedirects 20260803000116_AddGuestCheckoutAndMemberOnlyCoupons --project src/ECommerce.Persistence --startup-project src/ECommerce.API --no-build
git diff --check
```

Uygulama sırasında temiz bir geçici LocalDB üzerinde bütün migration zinciri başarıyla uygulanıp veritabanı silindi. Son doğrulamada build 0 warning/0 error, unit testler 443/443 ve integration testler 178/178 geçti. SQL scriptte guest tabloları, nullable Order owner, customer backfill ve `IsMemberOnly` doğrulandı.

OpenAPI çalışan API'den yeniden üretildi. Audit yeni guest route/schema'larını doğruladı; daha önce var olan global Bearer/public auth security override, auth success status/error response ve ProblemDetails schema eksikleri bu özelliğin kapsamı dışında açık contract drift olarak sürmektedir.

### Performans kararları

Guest sayaçları veritabanı yerine Redis atomik increment/TTL kullanır; fallback yalnız process içidir. Token/session/order grant ve idempotency sorguları hash/expiry/bileşik unique indexlerle desteklenir. Liste uçları en fazla 100 satırlık server-side pagination ve `AsNoTracking` kullanır. Access-link preflight sorgusu yalnız Order + CustomerSnapshot okur; Redis/Turnstile çağrısı açık SQL transaction dışında tutulur. Order detay grafiği kartesyen büyümeyi azaltmak için split query olarak yüklenir. Checkout doğruluk için kısa serializable transaction kullanır; SMTP ve payment provider ağ çağrıları transaction dışında kalır.

Ödeme provider sonucu, çağrıdan önce session/grant ile yetkilendirildikten sonra payment kimliği üzerinden uzlaştırılır; provider çağrısı sırasında session'ın claim edilmesi veya süresinin dolması başarılı harici tahsilatı veritabanında sahipsiz Pending bırakmaz.

## 12. Sorun giderme

| Belirti | Kontrol |
| --- | --- |
| İlk guest checkout 428 | Redis yoksa bu beklenen fallback davranışıdır; Turnstile tokenı gönderin. |
| Turnstile tokenıyla 503 | `Turnstile:SecretKey`, outbound HTTPS ve siteverify cevabını kontrol edin. |
| Checkout 403 | Browser/BFF `Origin` değeri `GuestProtection:TrustedOrigins` allowlist'inde mi kontrol edin. |
| Kupon 409 `coupon_members_only` | UI üyelik mesajı göstermeli; otomatik retry etmemeli. |
| Kargo 404/409 | Shipping method var mı, aktif mi ve checkout anında değişmiş mi kontrol edin. Ücret request'ten gelmez. |
| Magic-link e-postası yok | EmailOutbox satırı, lease/retry/dead-letter alanları, SMTP config ve Data Protection key-ring loglarını hassas tokenı yazmadan inceleyin. |
| Exchange 404 | Token kullanılmış, 30 dakikayı aşmış, revoke edilmiş veya fragment BFF body'ye taşınmamış olabilir. |
| Payment timeout | Aynı `Idempotency-Key` ile retry edin ve Order detail'i yeniden okuyun. |
| Başka order 404 | Bu güvenli ve kasıtlıdır; session yalnız grant verilen Order'a erişir. |
| Claim 403 | JWT hesap e-postası ile magic-link üzerinden doğrulanmış normalize e-posta eşleşmelidir. |

## 13. Kapsam dışı

Bu çalışmada UI sayfa/component kodu, yeni payment provider entegrasyonu, yeni stok yolu, e-posta ön doğrulaması veya kalıcı development/production migration uygulaması yapılmamıştır.
