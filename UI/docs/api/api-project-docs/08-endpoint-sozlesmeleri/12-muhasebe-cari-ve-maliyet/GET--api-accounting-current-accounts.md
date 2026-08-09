# GET /api/accounting/current-accounts

- İşlev alanı: **12 Muhasebe cari ve maliyet**
- İşlev: Kaynağı veya filtrelenmiş kaynak listesini okur.
- Operation ID: `GET-/api/accounting/current-accounts`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
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
    "items":  {
                  "id":  "00000000-0000-0000-0000-000000000001",
                  "code":  "string",
                  "type":  1,
                  "name":  "string",
                  "tradeName":  "string",
                  "nationalIdentityNumber":  "string",
                  "taxNumber":  "string",
                  "taxOffice":  "string",
                  "phoneNumber":  "string",
                  "email":  "string",
                  "country":  "string",
                  "city":  "string",
                  "district":  "string",
                  "neighborhood":  "string",
                  "addressLine":  "string",
                  "postalCode":  "string",
                  "isActive":  true,
                  "userId":  "string"
              },
    "pageNumber":  1,
    "pageSize":  1,
    "totalCount":  1,
    "totalPages":  1,
    "hasPreviousPage":  true,
    "hasNextPage":  true
}
```

