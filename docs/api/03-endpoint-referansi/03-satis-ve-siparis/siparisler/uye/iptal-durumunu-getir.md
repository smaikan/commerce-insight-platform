# GET /api/orders/{id}/cancellation

- Görev alanı: **Satış ve sipariş → Siparişler → Üye işlemleri**.

- Yetki: **User**.

Üyenin yalnız kendi siparişindeki en güncel cancellation operasyonunu polling için döndürür.

- Security: Bearer/JWT User.
- `200`: `OrderCancellationOperationDto`.
- `401 authentication_required` / `invalid_access_token`: JWT yok/geçersiz.
- `404 resource_not_found`: sipariş sahipliği yok, sipariş yok veya operasyon yok.

```json
{
  "operationId": "3470e031-3fc8-42af-9755-f0fcae2b06cb",
  "orderId": "3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26",
  "status": 3,
  "reversalType": 1,
  "createdAt": "2026-08-24T07:19:00Z",
  "updatedAt": "2026-08-24T07:21:00Z",
  "nextAttemptAt": null,
  "pollingUrl": "/api/orders/3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26/cancellation"
}
```

`status`: `0 Requested`, `1 Processing`, `2 ReconciliationPending`, `3 Completed`, `4 Failed`, `5 ManualReview`. `reversalType`: `0 Cancel`, `1 Refund`. Tarihler UTC'dir. Provider payment/transaction kimlikleri ve güvenli iç hata özeti public DTO'ya açılmaz.
