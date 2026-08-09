# POST /api/guest-orders/{id}/returns

Teslim edilmiş guest siparişi için mevcut iade/değişim kurallarıyla talep oluşturur.

## İstek sözleşmesi

- Cookie: `ecommerce_guest_orders`, `ecommerce_guest_csrf`.
- Header: trusted `Origin`, `X-Guest-CSRF`.

| Alan | Required | Nullable | Kural |
| --- | --- | --- | --- |
| id (route) | Evet | Hayır | Order GUID |
| type | Evet | Hayır | Refund veya Exchange enum değeri |
| items | Evet | Hayır | En az bir, benzersiz OrderItem |
| items[].orderItemId | Evet | Hayır | Bu siparişe ait olmalı |
| items[].quantity | Evet | Hayır | Kalan iade edilebilir miktar içinde |
| items[].replacementProductVariantId | Exchange'te evet | Diğerinde evet | Uygun aynı ürün varyantı |
| customerNote | Hayır | Evet | Serbest müşteri notu |

```http
POST /api/guest-orders/3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26/returns
Origin: https://store.example.com
X-Guest-CSRF: <csrf>
Cookie: ecommerce_guest_orders=<session>; ecommerce_guest_csrf=<csrf>
Content-Type: application/json

{"type":1,"items":[{"orderItemId":"5fa4aa48-bcd4-43c0-b685-27ddb892e696","quantity":1,"replacementProductVariantId":null}],"customerNote":"Paket açılmadı."}
```

Başarı `201 Created` ve `ReturnRequestDto`; cookie değişmez. Sipariş `ReturnRequested` olur, outbox bildirimi checkout cevabını bekletmez.

Hatalar: `400 validation_error`; `401 invalid_guest_access`; `403 invalid_guest_access` Origin/CSRF; `404 not_found`; `409 conflict` teslim edilmemiş sipariş, miktar veya replacement uygunsuzluğu; `428/429/503` uygulanmaz; `500` sonrası listeyi okuyup sonucu doğrula. Submit butonu tek uçuş olmalı; not/PII/cookie/CSRF loglanmamalıdır.
