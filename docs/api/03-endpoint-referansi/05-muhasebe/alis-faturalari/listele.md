# GET /api/accounting/purchase-invoices

- Görev alanı: **Muhasebe → Alış faturaları**.
- İşlev: listeler.
- Operation ID: `GET-/api/accounting/purchase-invoices`
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
      "currentAccountId": "00000000-0000-0000-0000-000000000001",
      "currentAccountName": "string",
      "invoiceNumber": "string",
      "invoiceDate": "2026-07-29T12:00:00Z",
      "currencyCode": "string",
      "status": 1,
      "grandTotalIncludingVat": 1
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

