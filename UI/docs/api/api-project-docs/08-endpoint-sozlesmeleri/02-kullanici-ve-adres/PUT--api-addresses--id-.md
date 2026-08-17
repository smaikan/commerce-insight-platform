# PUT /api/addresses/{id}

- Ýþlev alaný: **02 Kullanýcý ve adres**
- Ýþlev: Kaynaðýn güncellenebilir alanlarýný deðiþtirir.
- Operation ID: `PUT-/api/addresses/{id}`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Þema |
| --- | --- | --- | --- |
| `id` | path | Evet | string (uuid) |

## Request body

Aþaðýdaki örnek alan adlarýný camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `type` | integer (int32) | Evet |
| `title` | string | Evet |
| `firstName` | string | Evet |
| `lastName` | string | Evet |
| `phoneNumber` | string | Evet |
| `city` | string | Evet |
| district | string | Evet |
| neighborhood | string | Hayýr |

| 
eighborhood | string | Evet |

| `fullAddress` | string | Evet |
| `postalCode` | string | Hayýr |
| `isDefault` | boolean | Evet |

```json
{
    "type":  0,
    "title":  "string",
    "firstName":  "string",
    "lastName":  "string",
    "phoneNumber":  "string",
    "city":  "string",
    "district":  "string",
    "neighborhood":  "string",
    "neighborhood":  "string",
    "fullAddress":  "string",
    "postalCode":  "string",
    "isDefault":  true
}
```

## Baþarýlý response (200)

```json
{
    "id":  "00000000-0000-0000-0000-000000000001",
    "type":  0,
    "title":  "string",
    "firstName":  "string",
    "lastName":  "string",
    "phoneNumber":  "string",
    "city":  "string",
    "district":  "string",
    "neighborhood":  "string",
    "neighborhood":  "string",
    "fullAddress":  "string",
    "postalCode":  "string",
    "isDefault":  true,
    "createdAt":  "2026-07-29T12:00:00Z",
    "updatedAt":  "2026-07-29T12:00:00Z"
}
```




