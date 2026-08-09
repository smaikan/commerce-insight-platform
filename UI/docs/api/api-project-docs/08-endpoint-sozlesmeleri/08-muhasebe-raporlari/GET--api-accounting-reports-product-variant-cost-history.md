# GET /api/accounting/reports/product-variant-cost-history

- İşlev alanı: **08 Muhasebe raporları**
- İşlev: Filtrelenebilir muhasebe rapor verisini sayfalı olarak okur.
- Operation ID: `GET-/api/accounting/reports/product-variant-cost-history`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `PageNumber` | query | Hayır | integer (int32) |
| `PageSize` | query | Hayır | integer (int32) |
| `From` | query | Hayır | string (date-time) |
| `To` | query | Hayır | string (date-time) |
| `Id` | query | Hayır | string (uuid) |
| `HasSalesInvoice` | query | Hayır | boolean |
| `Search` | query | Hayır | string |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

```json
{
    "items":  {
                  "id":  "00000000-0000-0000-0000-000000000001",
                  "relatedId":  "00000000-0000-0000-0000-000000000001",
                  "number":  "string",
                  "name":  "string",
                  "date":  "2026-07-29T12:00:00Z",
                  "dueDate":  "2026-07-29T12:00:00Z",
                  "amount":  1,
                  "secondaryAmount":  1,
                  "tertiaryAmount":  1,
                  "quantity":  1,
                  "rate":  1,
                  "hasSalesInvoice":  true,
                  "currencyCode":  "string"
              },
    "pageNumber":  1,
    "pageSize":  1,
    "totalCount":  1,
    "totalPages":  1,
    "hasPreviousPage":  true,
    "hasNextPage":  true
}
```

