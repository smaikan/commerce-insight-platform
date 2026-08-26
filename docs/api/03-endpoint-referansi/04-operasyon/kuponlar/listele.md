# GET /api/coupons

- Görev alanı: **Operasyon → Kuponlar**.
- İşlev: listeler.
- Operation ID: `GET-/api/coupons`
- Yetki: **Admin**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `pageNumber` | query | Hayır | integer (int32) |
| `pageSize` | query | Hayır | integer (int32) |
| `isActive` | query | Hayır | boolean |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

```json
{
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000001",
      "code": "string",
      "description": "string",
      "discountType": 0,
      "discountValue": 1,
      "minimumOrderAmount": 1,
      "usageLimit": 1,
      "usedCount": 1,
      "startsAt": "2026-07-29T12:00:00Z",
      "expiresAt": "2026-07-29T12:00:00Z",
      "isMemberOnly": false,
      "isActive": true,
      "createdAt": "2026-07-29T12:00:00Z",
      "updatedAt": "2026-07-29T12:00:00Z"
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

`isMemberOnly=true` kupon yalnız authenticated checkout'ta kullanılabilir. `false` varsayılandır ve kupon guest/üye için diğer uygunluk kurallarına tabidir.

