# POST /api/guest-orders/{id}/payments

- Görev alanı: **Satış ve sipariş → Ödemeler → Misafir işlemleri**.

- Yetki: **Guest session**.

Guest siparişte mevcut ödeme orkestrasyonunu kullanarak idempotent ödeme denemesi başlatır.

## İstek sözleşmesi

- Authorization: JWT gerekmez; guest session grant zorunlu.
- Cookie: `ecommerce_guest_orders`, `ecommerce_guest_csrf`.
- Header: trusted `Origin`, cookie ile aynı `X-Guest-CSRF`, zorunlu `Idempotency-Key`.

| Alan | Konum | Required | Nullable | Kural |
| --- | --- | --- | --- | --- |
| id | route | Evet | Hayır | Order GUID |
| Idempotency-Key | header | Evet | Hayır | Aynı ödeme intent'inde korunur |
| provider | body | Evet | Hayır | API `PaymentProvider` enum değeri |

```http
POST /api/guest-orders/3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26/payments
Origin: https://store.example.com
X-Guest-CSRF: <csrf>
Idempotency-Key: pay_01K1...
Cookie: ecommerce_guest_orders=<session>; ecommerce_guest_csrf=<csrf>
Content-Type: application/json

{"provider":1}
```

## Başarılı cevap

```json
{
  "id": "5970fd54-d88f-49c0-b9ca-a7f20a58bf42",
  "orderId": "3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26",
  "provider": 1,
  "status": 2,
  "amount": 1299.9,
  "transactionId": "txn_safe_reference"
}
```

Cookie değişmez. Aynı key aynı ödeme sonucunu döndürür; timeout sonrası yeni key üretilmez.

## Hatalar, retry ve UI davranışı

- `400`: body/provider/key biçimi geçersiz.
- `401 invalid_guest_access`: access-link ile session yenile.
- `403 invalid_guest_access`: Origin/CSRF uyuşmaz; otomatik retry yapma.
- `404 not_found`: çapraz-order erişimi dahil.
- `409 conflict`: sıfır toplam, uygun olmayan durum, bekleyen ödeme veya provider yapılandırılmamış.
- `428/429`: bu uçta guest checkout challenge/limit uygulanmaz.
- `500/503`: sonucu belirsizse **aynı Idempotency-Key** ile yeniden dene ve siparişi yeniden oku.

Buton tek uçuş olmalı. Kart/veri sağlayıcı sırları, idempotency key, cookie, CSRF ve transaction ayrıntıları log/analytics'e yazılmaz.
