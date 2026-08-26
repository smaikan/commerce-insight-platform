# POST /api/returns

- Görev alanı: **Satış ve sipariş → İade ve değişim → Üye işlemleri**.
- İşlev: oluşturur.
- Operation ID: `POST-/api/returns`
- Yetki: **User**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `orderId` | string (uuid) | Evet |
| `type` | integer (int32) | Evet |
| `items` | array | Evet |
| `customerNote` | string | Hayır |

```json
{
  "orderId": "00000000-0000-0000-0000-000000000001",
  "type": 0,
  "items": [
    {
      "orderItemId": "00000000-0000-0000-0000-000000000001",
      "quantity": 1,
      "replacementProductVariantId": "00000000-0000-0000-0000-000000000001"
    }
  ],
  "customerNote": "string"
}
```

## Başarılı response (200)

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "returnNumber": "string",
  "orderId": "00000000-0000-0000-0000-000000000001",
  "type": 0,
  "status": 0,
  "refundTotal": 1,
  "customerNote": "string",
  "decisionNote": "string",
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000001",
      "orderItemId": "00000000-0000-0000-0000-000000000001",
      "productId": "string",
      "productVariantId": "00000000-0000-0000-0000-000000000001",
      "productTitle": "string",
      "variantSku": "string",
      "unitPrice": 1,
      "quantity": 1,
      "lineTotal": 1,
      "refundTotal": 1,
      "replacementProductVariantId": "00000000-0000-0000-0000-000000000001"
    }
  ],
  "approvedAt": "2026-07-29T12:00:00Z",
  "rejectedAt": "2026-07-29T12:00:00Z",
  "receivedAt": "2026-07-29T12:00:00Z",
  "completedAt": "2026-07-29T12:00:00Z",
  "createdAt": "2026-07-29T12:00:00Z"
}
```

