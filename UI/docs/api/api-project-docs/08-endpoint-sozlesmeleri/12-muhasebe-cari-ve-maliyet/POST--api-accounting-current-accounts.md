# POST /api/accounting/current-accounts

- Ýþlev alaný: **12 Muhasebe cari ve maliyet**
- Ýþlev: Yeni kaynak veya iþ akýþý adýmý oluþturur/baþlatýr.
- Operation ID: `POST-/api/accounting/current-accounts`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aþaðýdaki örnek alan adlarýný camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `code` | string | Evet |
| `type` | integer (int32) | Evet |
| `name` | string | Evet |
| `tradeName` | string | Hayýr |
| `nationalIdentityNumber` | string | Hayýr |
| `taxNumber` | string | Hayýr |
| `taxOffice` | string | Hayýr |
| `phoneNumber` | string | Hayýr |
| `email` | string | Hayýr |
| `country` | string | Hayýr |
| `city` | string | Hayýr |
| `district` | string | Hayýr |
| `neighborhood` | string | Hayýr |
| `addressLine` | string | Hayýr |
| `postalCode` | string | Hayýr |
| `userId` | string | Hayýr |

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
    "neighborhood":  "string",
    "neighborhood":  "string",
    "addressLine":  "string",
    "postalCode":  "string",
    "userId":  "string"
}
```

## Baþarýlý response (200)

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
    "neighborhood":  "string",
    "neighborhood":  "string",
    "addressLine":  "string",
    "postalCode":  "string",
    "isActive":  true,
    "userId":  "string"
}
```




