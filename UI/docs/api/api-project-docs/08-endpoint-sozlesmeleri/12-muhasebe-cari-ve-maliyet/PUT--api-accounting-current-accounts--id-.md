# PUT /api/accounting/current-accounts/{id}

- İşlev alanı: **12 Muhasebe cari ve maliyet**
- İşlev: Kaynağın güncellenebilir alanlarını değiştirir.
- Operation ID: `PUT-/api/accounting/current-accounts/{id}`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `id` | path | Evet | string (uuid) |

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `account` | object | Evet |
| `isActive` | boolean | Evet |

```json
{
    "account":  {
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
                    "userId":  "string"
                },
    "isActive":  true
}
```

## Başarılı response (200)

```json
{
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
}
```

