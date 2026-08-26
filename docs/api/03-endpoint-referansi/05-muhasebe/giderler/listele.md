# GET /api/accounting/expenses

- Görev alanı: **Muhasebe → Giderler**.
- İşlev: listeler.
- Operation ID: `GET-/api/accounting/expenses`
- Yetki: **Admin**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `PageNumber` | query | Hayır | integer (int32) |
| `PageSize` | query | Hayır | integer (int32) |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

```json
{
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000001",
      "categoryId": "00000000-0000-0000-0000-000000000001",
      "type": 1,
      "amountExcludingVat": 1,
      "vatRate": 1,
      "vatAmount": 1,
      "totalAmountIncludingVat": 1,
      "expenseDate": "2026-07-29T12:00:00Z",
      "description": "string"
    }
  ],
  "pageNumber": 1,
  "pageSize": 1,
  "totalCount": 1,
  "totalPages": 1,
  "hasPreviousPage": true,
  "hasNextPage": true
}
```

