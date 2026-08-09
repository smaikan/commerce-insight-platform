# POST /api/guest-orders/access-links

Farklı cihaz veya süresi dolan session için magic-link e-postası ister. Order number + email yalnız kayıt bulma/gönderim girdisidir; yetki sağlamaz.

## Sözleşme

- Authorization/session: gerekmez.
- `Origin`: zorunlu trusted storefront.
- Cookie/header: auth cookie gerekmez; token header yoktur.

| Body | Tip | Required | Nullable |
| --- | --- | --- | --- |
| orderNumber | string | Evet | Hayır |
| email | email | Evet | Hayır |

```http
POST /api/guest-orders/access-links
Origin: https://store.example.com
Content-Type: application/json

{"orderNumber":"ORD-6F7951F775D04EF5B7E8","email":"ada@example.com"}
```

## Başarı ve gizlilik

Kayıt eşleşse de eşleşmese de `202`:

```json
{"message":"Sipariş eşleşirse erişim bağlantısı e-posta kuyruğuna alındı."}
```

Eşleşmede 256 bit token hash’i DB’ye, Data Protection ile korunmuş raw token outbox’a yazılır. Worker 30 dakikalık tek kullanımlık fragment link gönderir; request SMTP’yi beklemez. Cookie değişmez.

## Hatalar/retry

- `400 validation_error`: body eksik; form alanını düzelt.
- `401`: beklenmez; endpoint publictir.
- `403 invalid_guest_access`: Origin reddi.
- `404`: güvenlik nedeniyle order/email eşleşmemesi 404 dönmez.
- `409`: beklenmez; eşleşme e-posta varlığını açmaz.
- `428`: bu endpointte Turnstile checkout challenge uygulanmaz.
- `429 guest_checkout_rate_limited`: order başına 3/saat veya IP 10/saat; otomatik resend durdur.
- `500/503`: güvenli genel hata; PII’yi loglama, kullanıcıya daha sonra retry sun.

Loading sırasında resend butonu disable edilir. Order number/e-posta, raw token, cookie ve link fragment analytics’e gönderilmez.
