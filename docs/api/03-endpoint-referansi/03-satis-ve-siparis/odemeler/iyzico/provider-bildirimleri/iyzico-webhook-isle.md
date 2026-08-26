# POST /api/payments/iyzico/webhook

- Görev alanı: **Satış ve sipariş → Ödemeler → iyzico → Provider bildirimleri**.

- Yetki: **Provider**.
- Header: zorunlu `X-IYZ-SIGNATURE-V3`.
- Başarı: `204 No Content`.

```json
{
  "iyziEventType": "CHECKOUT_FORM_AUTH",
  "iyziPaymentId": 28157797,
  "token": "provider-token",
  "paymentConversationId": "local-payment-conversation",
  "status": "SUCCESS"
}
```

API HPP V3 imzasını sabit zamanlı karşılaştırır ve ardından token ile retrieve yapar. Webhook gövdesindeki `SUCCESS` tek başına Paid yapmaz.

Hatalar: `400` body, `401` V3 imza, `404` token, `409` event/eşleşme. Akış idempotenttir; callback önce tamamladıysa yine `204` döner.
