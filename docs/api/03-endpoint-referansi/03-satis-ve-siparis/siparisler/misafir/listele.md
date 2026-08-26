# GET /api/guest-orders

- Görev alanı: **Satış ve sipariş → Siparişler → Misafir işlemleri**.

- Yetki: **Guest session**.

Doğrulanmış guest session'ın erişim grant'i bulunan siparişlerini listeler. Cevap `Cache-Control: no-store` olarak ele alınmalıdır.

## İstek sözleşmesi

- Authorization: JWT gerekmez.
- Cookie: `ecommerce_guest_orders` zorunlu; 64 karakterlik, 256 bit session tokenıdır.
- Header/CSRF: GET olduğu için `Origin` ve `X-Guest-CSRF` gerekmez.

| Query | Required | Nullable | Kural |
| --- | --- | --- | --- |
| pageNumber | Hayır | Hayır | Varsayılan 1, en az 1 |
| pageSize | Hayır | Hayır | Varsayılan 20, 1-100 |

```http
GET /api/guest-orders?pageNumber=1&pageSize=20
Cookie: ecommerce_guest_orders=<session>
```

## Başarılı cevap

```json
{
  "items": [
    {
      "id": "3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26",
      "orderNumber": "ORD-20260803-001",
      "status": 1,
      "grandTotal": 1299.9,
      "createdAt": "2026-08-03T12:00:00Z"
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1
}
```

Cookie değişmez. BFF cevabı cache'lememeli ve `no-store` ile iletmelidir.

## Hatalar, retry ve UI davranışı

- `400`: geçersiz sayfalama; kullanıcı girdisini düzelt.
- `401 invalid_guest_access`: cookie yok/geçersiz/süresi dolmuş; access-link ekranına yönlendir.
- `403`: bu GET'te beklenmez.
- `404`: liste için beklenmez.
- `409/428/429/503`: bu uçta normal akış değildir.
- `500`: sınırlı backoff ile retry sunulabilir.

Otomatik sayfalama isteği önceki isteği iptal edebilir. Session cookie, sipariş müşteri PII'si ve cevap gövdesi log/analytics'e yazılmamalıdır.
