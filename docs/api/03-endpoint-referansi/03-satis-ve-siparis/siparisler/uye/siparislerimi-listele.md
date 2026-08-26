# GET /api/orders/mine

- Görev alanı: **Satış ve sipariş → Siparişler → Üye işlemleri**.
- İşlev: Siparislerimi listeler.
- Operation ID: `GET-/api/orders/mine`
- Yetki: **User**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `PageNumber` | query | Hayır | integer (int32) |
| `PageSize` | query | Hayır | integer (int32) |
| `Status` | query | Hayır |  |
| `CreatedFromUtc` | query | Hayır | string (date-time) |
| `CreatedToUtc` | query | Hayır | string (date-time) |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

```json
{
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000001",
      "orderNumber": "string",
      "status": 0,
      "grandTotal": 1,
      "itemCount": 1,
      "createdAt": "2026-07-29T12:00:00Z",
      "paidAt": "2026-07-29T12:00:00Z"
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

