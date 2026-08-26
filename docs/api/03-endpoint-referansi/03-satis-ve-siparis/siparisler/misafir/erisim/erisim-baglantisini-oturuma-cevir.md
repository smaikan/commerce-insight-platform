# POST /api/guest-orders/access/exchange

- Görev alanı: **Satış ve sipariş → Siparişler → Misafir işlemleri → Misafir sipariş erişimi**.

- Yetki: **Public + Origin**.

Magic-link fragment tokenını tek kullanımlık guest session/order grant’ine çevirir.

## Sözleşme

- Authorization: gerekmez.
- `Origin`: trusted storefront zorunlu.
- Cookie: mevcut geçerli `ecommerce_guest_orders` varsa reuse; yoksa yeni session/CSRF cookie set edilir.
- Token URL query’sinde değil body’dedir.

```http
POST /api/guest-orders/access/exchange
Origin: https://store.example.com
Content-Type: application/json

{"token":"<64-karakter-256-bit-token>"}
```

| Body | Required | Nullable | Sınır |
| --- | --- | --- | --- |
| token | Evet | Hayır | Raw token yalnız bu istek gövdesinde |

## Başarı

```json
{
  "orderId": "3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26",
  "sessionExpiresAt": "2026-08-10T12:00:00Z"
}
```

Yeni session’da `ecommerce_guest_orders` ve `ecommerce_guest_csrf` Secure/HttpOnly/SameSite=Lax/Path=/api cookie set edilir. Token tüketilir, session e-posta hash’i doğrulanır ve yalnız linkteki Order grant’i verilir.

## Hatalar/retry

- `400 validation_error`: token boş/biçimsiz.
- `401`: normalde dönmez; exchange session öncesidir.
- `403 invalid_guest_access`: Origin reddi.
- `404 invalid_guest_access`: token yok, süresi dolmuş, kullanılmış veya iptal edilmiş; yeni access-link iste.
- `409`: nadir grant concurrency; session ile listeyi yeniden oku.
- `428/429`: checkout challenge uygulanmaz; access-link rate limit ayrı uçtadır.
- `500/503`: fragment tokenı loglamadan güvenli hata; kullanıcıdan yeni link istemesini öner.

BFF fragment’ı browser URL’sinden okuyup mümkün olan ilk anda temizler, body ile upstream’e taşır. Token Client Component state’inde kalıcı tutulmaz, log/analytics/screenshot’a girmez; otomatik sonsuz retry yapılmaz.
