# POST /api/guest-orders/{id}/payments/iyzico/checkout-form

- Yetki: `AllowAnonymous` + aktif guest order session grant.
- Cookie: `ecommerce_guest_orders`, `ecommerce_guest_csrf`.
- Header: trusted `Origin`, cookie ile aynı `X-Guest-CSRF`, zorunlu `Idempotency-Key`.
- Request body: yok; kart verisi gönderilmez.

```http
POST /api/guest-orders/{id}/payments/iyzico/checkout-form
Origin: https://store.example.com
X-Guest-CSRF: <csrf>
Idempotency-Key: pay_01K2B7N8F4QW6R9M
Cookie: ecommerce_guest_orders=<session>; ecommerce_guest_csrf=<csrf>
```

Başarı `201 CheckoutFormSessionDto` ve üye endpointiyle aynı şemadır. Cookie değişmez.

Hatalar: `400`, `401` guest session, `403` Origin/CSRF, `404` başka session order'ı, `409` provider/order/pending-payment kuralı. Retry aynı ödeme niyetinde aynı key ile yapılır.

Ayrıntı: [iyzico CheckoutForm sözleşmesi](IYZICO-CHECKOUTFORM-SOZLESMESI.md).
