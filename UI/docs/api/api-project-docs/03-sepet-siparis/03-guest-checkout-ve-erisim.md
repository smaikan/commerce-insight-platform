# Guest Checkout ve Sipariş Erişimi

## Değişmez kurallar

- Ortak/default guest User yoktur; `Order.UserId=null` kalır.
- Ad, soyad, e-posta, telefon, shipping address, aktif shipping method ve idempotency key zorunludur.
- Billing address opsiyoneldir; yoksa shipping snapshot’tan Billing snapshot üretilir.
- Checkout öncesi e-posta doğrulaması yapılmaz.
- Fiyat, vergi, kupon indirimi, shipping fee, stok ve toplam frontend’den alınmaz.
- Guest ve üye aynı checkout orkestratörü ve tek StockMovement yolunu kullanır.

## Tam guest checkout isteği

```http
POST /api/cart/checkout/guest
Origin: https://store.example.com
Idempotency-Key: 01J4GUESTCHECKOUT8R6F3K
X-Turnstile-Token: <yalnız istendiğinde>
Cookie: ecommerce_guest_cart=<HttpOnly; browser taşır>
```

```json
{
  "expectedCartConcurrencyToken": "77a50a8f-8dc1-4aa7-aed0-71ac4e58a8bb",
  "customer": {
    "firstName": "Ada",
    "lastName": "Lovelace",
    "email": "ada@example.com",
    "phoneNumber": "+905551112233"
  },
  "shippingAddress": {
    "title": "Ev",
    "firstName": "Ada",
    "lastName": "Lovelace",
    "phoneNumber": "+905551112233",
    "city": "İstanbul",
    "district": "Kadıköy",
    "fullAddress": "Örnek Sokak 1",
    "postalCode": "34000"
  },
  "billingAddress": null,
  "shippingMethodId": "893fdb48-e9cf-4c8c-a94c-3b989697f204",
  "couponCode": "WELCOME10"
}
```

`billingAddress` shipping ile aynı şekildedir. `shippingMethodId` bulunamazsa 404, pasifse 409 döner ve hiçbir Order/StockMovement oluşmaz. Backend shipping method adını ve sabit ücretini snapshot eder.

## Kupon

`Coupon.IsMemberOnly=false` varsayılandır ve guest/üye kullanabilir. `true` ise indirim hesabından önce tam `409 coupon_members_only` döner. UI “Bu kupon yalnızca üyeler içindir” gösterir; otomatik retry yapmaz, login veya kuponu kaldırma seçeneği sunar. Guest CouponUsage `UserId=null`, `OrderId=<guest order>` olur; iptalde mevcut release akışı çalışır, claim’de UserId atanır.

## Aynı cihaz session

İlk başarılı guest checkout API tarafından iki cookie üretir:

| Cookie | Süre | Özellik |
| --- | --- | --- |
| `ecommerce_guest_orders` | 7 gün | 256 bit session; Secure/HttpOnly/SameSite=Lax/Path=/api |
| `ecommerce_guest_csrf` | 7 gün | 256 bit CSRF; Secure/HttpOnly/SameSite=Lax/Path=/api |

Veritabanında yalnız SHA-256 hash bulunur. Aynı cihazda session grant ile list/detail/payment/cancel/return erişimi mümkündür. Sipariş numarası ve e-posta yetki değildir. Session’ın başka Order ID’ye erişimi 404’tür.

Mutasyonlarda BFF HttpOnly CSRF cookie’yi server-side okur ve `X-Guest-CSRF` header’ına ekler; browser Origin’i BFF ve API allowlist’i tarafından doğrulanır.

## Magic link ve farklı cihaz

Checkout transaction’ı 256 bit token hash’i ve Data Protection ile korunmuş outbox tokenı üretir. SMTP background worker tokenı yalnız gönderim anında çözer. Link 30 dakika ve tek kullanımlıktır. Token query string yerine storefront URL fragment’ındadır:

```text
https://store.example.com/guest-orders/access#token=<raw-token>
```

Client fragment’ı okur, URL’den temizler ve same-origin BFF exchange endpointine body ile gönderir. BFF tokenı loglamaz. `POST /api/guest-orders/access/exchange` linki tüketir, e-posta hash’ini session’da doğrular ve yalnız ilgili Order grant’ini verir. Süre dolmuş/kullanılmış token 404’tür. Resend `POST /api/guest-orders/access-links` ile orderNumber+email alır fakat her zaman aynı 202 cevabını verir; bu değerler doğrudan yetki sağlamaz.

## Self-service endpointleri

| Endpoint | Gereksinim |
| --- | --- |
| POST `/api/guest-orders/access-links` | Origin; uniform 202 |
| POST `/api/guest-orders/access/exchange` | Origin; raw token body |
| GET `/api/guest-orders` | session cookie; no-store |
| GET `/api/guest-orders/{id}` | session grant; no-store |
| POST `/api/guest-orders/{id}/payments` | session + CSRF + Origin + Idempotency-Key |
| POST `/api/guest-orders/{id}/cancel` | session + CSRF + Origin |
| GET/POST `/api/guest-orders/{id}/returns` | session grant; POST ayrıca CSRF + Origin |
| GET `/api/guest-orders/{id}/returns/{returnId}` | session/order/return zinciri |
| POST `/api/guest-orders/claim` | JWT + session + CSRF + Origin + doğrulanmış aynı e-posta |

## Claim

Claim için kullanıcı JWT ile giriş yapmış olmalı; hesap e-postasının normalize değeri magic-link ile doğrulanmış session e-postasıyla eşleşmelidir. Backend aynı e-postadaki bütün sahipsiz guest Order’ları tek transaction’da User’a bağlar; ilgili ReturnRequest ve CouponUsage UserId alanlarını günceller, guest grant/magic-link kayıtlarını iptal eder ve cookie’leri siler. Başka e-posta veya doğrulanmamış session 403’tür.

Guest review/rating claim öncesi mümkün değildir. Claim sonrası da yalnız teslim edilmiş satın alma kontrolü geçerse mümkündür.

## Limit, retry ve recovery

- Üçüncü 10 dakikalık checkout denemesinde 428 challenge gerekir.
- IP 5/15dk, session+email 5/saat, aktif unpaid reservation 3 sınırdır.
- Magic link 3/order/saat ve 10/IP/saat sınırıdır.
- Redis kesintisinde local fallback + zorunlu Turnstile; Turnstile servisi yoksa 503.
- 409 cart concurrency’de cart yeniden okunur.
- 409 member-only kupon retry edilmez.
- 428’de challenge tamamlanıp aynı body/key gönderilir.
- 429’da kontrollü beklenir.
- Timeout’ta aynı checkout intent/body/key korunur.

## Next.js BFF kesin topolojisi

1. Browser yalnız storefront same-origin Route Handler’a çağrı yapar.
2. Route Handler browser `Origin` değerini allowlist ile doğrular.
3. Yalnız allowlist cookie/header’ları ASP.NET API’ye taşır; Host/Authorization/proxy header’ları körlemesine iletmez.
4. Upstream `Set-Cookie` storefront origin altında Secure/HttpOnly/SameSite=Lax olarak yeniden yazar.
5. CSRF cookie server-side okunur, `X-Guest-CSRF` upstream header’ına eklenir.
6. Magic token fragment’tan BFF exchange body’sine aktarılır; query/log/analytics’e girmez.
7. Server Component kendi Route Handler’ına HTTP yapmaz; ortak server-only API fonksiyonunu çağırır.
8. Cart/order/guest detail cevapları `no-store`; cookie/token Client Component, localStorage, DOM veya serialized prop’a çıkmaz.
