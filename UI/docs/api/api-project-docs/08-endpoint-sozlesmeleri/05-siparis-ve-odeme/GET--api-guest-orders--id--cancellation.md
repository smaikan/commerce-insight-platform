# GET /api/guest-orders/{id}/cancellation

Guest session grant'inin erişebildiği siparişin en güncel cancellation operasyonunu no-store polling cevabı olarak döndürür.

- Security: geçerli `ecommerce_guest_orders` session cookie. GET olduğu için CSRF header gerekmez.
- `200`: `OrderCancellationOperationDto`; alanlar/numeric enumlar üye polling belgesiyle aynıdır.
- `401 invalid_guest_access`: session yok/geçersiz.
- `404 resource_not_found`: grant kapsamı dışında, sipariş yok veya operasyon yok.
- Response `Cache-Control: no-store` davranışındadır.

```json
{
  "operationId": "3470e031-3fc8-42af-9755-f0fcae2b06cb",
  "orderId": "3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26",
  "status": 2,
  "reversalType": 0,
  "createdAt": "2026-08-24T07:19:00Z",
  "updatedAt": "2026-08-24T07:19:03Z",
  "nextAttemptAt": "2026-08-24T07:20:03Z",
  "pollingUrl": "/api/guest-orders/3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26/cancellation"
}
```
