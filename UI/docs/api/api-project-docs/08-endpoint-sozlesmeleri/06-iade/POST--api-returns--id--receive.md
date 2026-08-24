# POST /api/returns/{id}/receive

Fiziksel iadenin karar verilmeden önce depoya ulaştığını kaydeder. `AdminOnly` ve Bearer kimlik doğrulaması zorunludur.

## Sözleşme

- Path `id`: zorunlu UUID.
- Request body yoktur.
- Yeni akışta yalnız `Requested (0)` kabul edilir; sonuç `Received (3)` ve `receivedAt` UTC olur.
- İlişkili Order `ReturnRequested (8)` kalır.
- Yeni akışta stok hareketi, provider refund, Payment veya kupon değişikliği oluşmaz.
- Deployment öncesinden kalan `Approved` kayıtların receive davranışı sınırlı legacy uyumluluğu olarak korunur.

## 200 response

```json
{
  "id": "d5349bf6-2d2c-46de-a9ea-13c9248d9d19",
  "returnNumber": "RET-20260823-7F91C2",
  "orderId": "8b532be3-3260-4547-b72e-28f62846ac04",
  "type": 0,
  "status": 3,
  "refundTotal": 749.90,
  "customerNote": "Beden uymadı",
  "decisionNote": null,
+  "items": [
    {
      "id": "f1dc42cc-4846-4726-9dc9-f4234db544d7",
      "orderItemId": "6f5ab3f2-338f-47f1-b87c-fbce9e409d1a",
      "productId": "P00042",
      "productVariantId": "839f2663-380b-4bf2-883f-81b98e9a7784",
      "productTitle": "Keten Gömlek",
      "variantSku": "KG-M-BEJ",
      "unitPrice": 749.90,
      "quantity": 1,
      "lineTotal": 749.90,
      "refundTotal": 749.90,
      "replacementProductVariantId": null
    }
  ],
  "approvedAt": null,
  "rejectedAt": null,
  "receivedAt": "2026-08-23T10:00:00Z",
  "completedAt": null,
  "createdAt": "2026-08-22T14:30:00Z"
}
```

## Hatalar

| HTTP | ProblemDetails `code` |
| --- | --- |
| 400 | `validation_error`, `bad_request`, `business_rule_violation` |
| 401 | `authentication_required`, `invalid_access_token` |
| 403 | `forbidden` |
| 404 | `resource_not_found` |
| 409 | Geçersiz durum için `return_status_transition_invalid`; gerçek yazma yarışı için `concurrency_conflict` |
