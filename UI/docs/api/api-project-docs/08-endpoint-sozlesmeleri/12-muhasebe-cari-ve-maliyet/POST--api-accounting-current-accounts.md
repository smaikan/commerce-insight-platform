# POST /api/accounting/current-accounts

- İşlev alanı: **12 Muhasebe cari ve maliyet**
- İşlev: Yeni kaynak veya iş akışı adımı oluşturur/başlatır.
- Operation ID: `POST-/api/accounting/current-accounts`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `code` | string | Evet |
| `type` | integer (int32) | Evet |
| `name` | string | Evet |
| `tradeName` | string | Hayır |
| `nationalIdentityNumber` | string | Hayır |
| `taxNumber` | string | Hayır |
| `taxOffice` | string | Hayır |
| `phoneNumber` | string | Hayır |
| `email` | string | Hayır |
| `country` | string | Hayır |
| `city` | string | Hayır |
| `district` | string | Hayır |
| `neighborhood` | string | Hayır |
| `addressLine` | string | Hayır |
| `postalCode` | string | Hayır |
| `userId` | string | Hayır |

```json
{
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

