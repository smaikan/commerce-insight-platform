# POST /api/orders/{id}/payments/iyzico/checkout-form

- Yetki: User/JWT ve order sahipliği.
- Rate limit: `payments`, varsayılan 10/dakika.
- Request body: yok; kart verisi gönderilmez.
- Header: `Idempotency-Key` zorunlu, 16–80 karakter, `[A-Za-z0-9_-]+`.

```http
POST /api/orders/3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26/payments/iyzico/checkout-form
Authorization: Bearer <access-token>
Idempotency-Key: pay_01K2B7N8F4QW6R9M
```

## 201 CheckoutFormSessionDto

```json
{
  "paymentId": "5970fd54-d88f-49c0-b9ca-a7f20a58bf42",
  "orderId": "3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26",
  "provider": 1,
  "status": 0,
  "amount": 1299.90,
  "paymentPageUrl": "https://sandbox-cpp.iyzipay.com?token=...&lang=tr",
  "expiresAt": "2026-08-16T12:30:00Z"
}
```

Frontend yalnız response içindeki `paymentPageUrl` adresine yönlenir. Aynı key tekrarında aynı form döner.

Hatalar: `400` validation, `401` JWT, `404` sahip olunmayan order dahil, `409` provider kapalı/sıfır toplam/uygunsuz durum/açık ödeme. Ortak gövde `ProblemDetails`tir.

Ayrıntı: [iyzico CheckoutForm sözleşmesi](IYZICO-CHECKOUTFORM-SOZLESMESI.md).
