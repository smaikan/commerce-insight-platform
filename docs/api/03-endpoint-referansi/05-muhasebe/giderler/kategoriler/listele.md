# GET /api/accounting/expenses/categories

- Görev alanı: **Muhasebe → Giderler → Kategoriler**.
- İşlev: listeler.
- Operation ID: `GET-/api/accounting/expenses/categories`
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
      "code": "string",
      "name": "string",
      "isActive": true
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

