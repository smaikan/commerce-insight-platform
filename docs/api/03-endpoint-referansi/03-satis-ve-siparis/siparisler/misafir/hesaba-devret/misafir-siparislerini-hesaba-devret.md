# POST /api/guest-orders/claim

- Görev alanı: **Satış ve sipariş → Siparişler → Misafir işlemleri → Hesaba devretme**.

- Yetki: **User**.

Magic-link ile doğrulanmış guest session'daki e-posta ile JWT hesabının normalize e-postası eşleşirse aynı e-postadaki bütün sahipsiz guest siparişlerini atomik olarak hesaba bağlar.

## İstek sözleşmesi

- Authorization: `Bearer <JWT>` zorunlu.
- Cookie: `ecommerce_guest_orders`, `ecommerce_guest_csrf`.
- Header: trusted `Origin`, `X-Guest-CSRF`.
- Body/route/query: yok.

```http
POST /api/guest-orders/claim
Authorization: Bearer <jwt>
Origin: https://store.example.com
X-Guest-CSRF: <csrf>
Cookie: ecommerce_guest_orders=<session>; ecommerce_guest_csrf=<csrf>
```

## Başarılı cevap

```json
{
  "claimedOrderCount": 2
}
```

Order.UserId ile ilgili nullable ReturnRequest.UserId/CouponUsage.UserId birlikte güncellenir; guest grant/link/session iptal edilir. Response guest session ve CSRF cookie'lerini siler. Claim sonrası üye `/api/orders/mine` akışını kullanır; teslim edilmiş ürün review/rating uygunluğu ancak claim sonrası hesap üzerinden değerlendirilebilir.

## Hatalar, retry ve UI davranışı

- `400`: istek biçimi.
- `401`: JWT veya guest session geçersiz; iki kimlik de gereklidir.
- `403 invalid_guest_access`: Origin/CSRF ya da doğrulanmış guest e-postası hesap e-postasıyla eşleşmiyor.
- `404`: hesap bulunamadı.
- `409`: eşzamanlı claim çakışması; üye sipariş listesini yeniden oku.
- `428/429/503`: guest checkout koruması uygulanmaz.
- `500`: üyeye ait siparişleri ve guest session durumunu yeniden okuyup sonucu doğrula; yeni session üretip kör retry yapma.

Loading sırasında claim tek uçuş olmalıdır. JWT, cookie, CSRF, e-posta hash'i veya müşteri PII'si log/analytics'e yazılmaz.
