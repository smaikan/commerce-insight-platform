# POST /api/returns/{id}/complete

- Görev alanı: **Satış ve sipariş → İade ve değişim → Yönetim**.

- Yetki: **Admin**.

Yalnız deployment öncesindeki iade kayıtları için geriye dönük uyumluluk endpointidir. Yeni yaşam döngüsünde kullanılmaz. `AdminOnly` ve Bearer kimlik doğrulaması zorunludur.

## Sözleşme

- Path `id`: zorunlu UUID.
- Request body yoktur.
- Yalnız eski akışta onaylanmış (`approvedAt` dolu) ve sonradan teslim alınmış `Received (3)` kayıt kabul edilir.
- Sonuç `Completed (4)` olur ve `completedAt` UTC yazılır.
- Eski exchange kaydının stok işlemi bu uyumluluk adımında atomik tamamlanır. Eski refund stok girişi receive aşamasında zaten yazılmıştır ve tekrarlanmaz.
- Yeni `Requested -> Received -> Approved/Rejected` kayıtları bu endpointi kullanamaz.

## 200 response

```json
{
  "id": "d5349bf6-2d2c-46de-a9ea-13c9248d9d19",
  "returnNumber": "RET-LEGACY-4C81",
  "orderId": "8b532be3-3260-4547-b72e-28f62846ac04",
  "type": 1,
  "status": 4,
  "refundTotal": 0,
  "customerNote": null,
  "decisionNote": "Legacy approval",
  "items": [
    {
      "id": "f1dc42cc-4846-4726-9dc9-f4234db544d7",
      "orderItemId": "6f5ab3f2-338f-47f1-b87c-fbce9e409d1a",
      "productId": "P00042",
      "productVariantId": "839f2663-380b-4bf2-883f-81b98e9a7784",
      "productTitle": "Keten Gömlek",
      "variantSku": "KG-M-BEJ",
      "unitPrice": 749.9,
      "quantity": 1,
      "lineTotal": 749.9,
      "refundTotal": 0,
      "replacementProductVariantId": "b0346fd2-8fe8-49f3-ad74-5489eff1c62a"
    }
  ],
  "approvedAt": "2026-08-20T09:00:00Z",
  "rejectedAt": null,
  "receivedAt": "2026-08-21T10:00:00Z",
  "completedAt": "2026-08-23T10:00:00Z",
  "createdAt": "2026-08-19T15:00:00Z"
}
```

## Hatalar

| HTTP | ProblemDetails `code` |
| --- | --- |
| 400 | `validation_error`, `bad_request`, `business_rule_violation` |
| 401 | `authentication_required`, `invalid_access_token` |
| 403 | `forbidden` |
| 404 | `resource_not_found` |
| 409 | Yeni veya uygun olmayan kayıtta `return_status_transition_invalid`; gerçek yazma yarışında `concurrency_conflict` |
