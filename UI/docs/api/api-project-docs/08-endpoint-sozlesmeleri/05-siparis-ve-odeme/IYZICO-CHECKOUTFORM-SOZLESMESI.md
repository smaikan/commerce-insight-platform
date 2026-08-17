# iyzico CheckoutForm sandbox sözleşmesi

Bu akışta kart numarası, son kullanma tarihi ve CVV ECommerce API'ye gönderilmez. API, sipariş snapshot'larından iyzico hosted ödeme oturumu üretir; frontend yalnız dönen `paymentPageUrl` adresine yönlenir.

## Frontend akışı

1. Checkout ile `OrderDto` oluşturulur ve stok rezervasyonu açılır.
2. Frontend aynı ödeme niyeti boyunca değişmeyen, 16–80 karakterlik bir `Idempotency-Key` üretir.
3. Üye veya guest initialize endpointi çağrılır.
4. API önce `Payment(status=Pending, provider=Iyzico)` kaydını commit eder, iyzico initialize çağrısını transaction dışında yapar.
5. `201 CheckoutFormSessionDto` içindeki `paymentPageUrl` adresine yönlenilir.
6. iyzico işlem sonunda `POST /api/payments/iyzico/callback` adresine `token` gönderir.
7. API callback gövdesine güvenmez; token ile retrieve yapar. Yanıt imzası, `paymentStatus`, `fraudStatus`, basket/conversation kimliği, TRY ve tutarlar doğrulanır.
8. Yalnız `status=success`, `paymentStatus=SUCCESS`, `fraudStatus=1` ve bütün yerel eşleşmeler geçerliyse Payment/Order atomik olarak `Paid` olur.
9. API tarayıcıyı `Iyzico:ReturnUrl` adresine `paymentId`, `orderId`, `status` query alanlarıyla `303` yönlendirir. Frontend bu query'yi finansal kaynak saymaz; sahiplik korumalı Order GET ile güncel durumu okur.
10. `X-IYZ-SIGNATURE-V3` imzalı webhook aynı idempotent retrieve akışını çalıştırır.

## CheckoutFormSessionDto

| Alan | Tip | Required | Nullable | Açıklama |
| --- | --- | --- | --- | --- |
| paymentId | uuid | Evet | Hayır | Yerel Payment kimliği |
| orderId | uuid | Evet | Hayır | Yerel Order kimliği |
| provider | int32 enum | Evet | Hayır | `Iyzico=1` |
| status | int32 enum | Evet | Hayır | `Pending=0`, `Paid=1`, `Failed=2` |
| amount | decimal | Evet | Hayır | Backend `Order.GrandTotal` değeri |
| paymentPageUrl | string | Evet | Evet | Başarılı initialize'da mutlak iyzico URL'si |
| expiresAt | date-time | Evet | Evet | Form tokenının UTC son kullanma zamanı |

Provider tokenı, API/secret key, signature, kart verisi ve provider hata ayrıntısı DTO'ya çıkmaz.

## Idempotency, durum ve güvenlik

- Aynı order + aynı key mevcut formu döndürür; iyzico ikinci kez çağrılmaz.
- Farklı key, mevcut bir `Pending` ödeme varken `409 conflict` alır.
- Callback tutar/kimlik eşleşmezse `409` döner ve Payment/Order değişmez.
- `fraudStatus=0` kesin başarı değildir; ödeme `Pending` kalır. `-1` başarısızdır, `1` başarı koşullarından biridir.
- Rezervasyon background reconciliation'ı aynı retrieve kontrolünü kullanır: paid siparişi korur, kesin failure rezervasyonu bırakır, belirsiz sonuç otomatik iptal edilmez.
- Guest initialize, `ecommerce_guest_orders` ve `ecommerce_guest_csrf` cookie'leri, trusted `Origin` ve `X-Guest-CSRF` ister.
- Callback/webhook iyzico tarafından çağrılır; cookie/CSRF yerine retrieve response signature ve webhook V3 signature doğrulanır.

## Sandbox configuration

Şablon `API/.env.example` dosyasındadır:

```dotenv
IYZICO__ENABLED=true
IYZICO__BASEURL=https://sandbox-api.iyzipay.com
IYZICO__APIKEY=<sandbox-api-key>
IYZICO__SECRETKEY=<sandbox-secret-key>
IYZICO__CALLBACKURL=https://public-api.example.com/api/payments/iyzico/callback
IYZICO__RETURNURL=http://localhost:3000/checkout/payment-result
IYZICO__SANDBOXBUYERIDENTITYNUMBER=11111111111
```

`.env` ASP.NET tarafından kendiliğinden okunmaz; container, IDE veya process launcher değerleri environment variable olarak API sürecine aktarmalıdır. Yerel `.env` git-ignore kapsamındadır. `CallbackUrl`, iyzico'nun internetten erişebildiği HTTPS URL olmalıdır.

Bu sürüm sandbox-only'dir: API çağrıları yalnız `https://sandbox-api.iyzipay.com` adresine yapılır; hosted CheckoutForm yönlendirmesinde iyzico'nun resmi `https://sandbox-cpp.iyzipay.com` origin'i kabul edilir. Sandbox kimlik numarası canlı veri değildir. Canlıya geçmeden önce gerçek alıcı kimliği checkout sözleşmesine güvenli şekilde eklenmeli ve fallback kaldırılmalıdır.

Fatura/e-fatura, kart saklama, refund ve provider cancel entegrasyonu kapsam dışıdır.
