# POST /api/addresses

- İşlev alanı: **02 Kullanıcı ve adres**
- İşlev: Yeni kaynak veya iş akışı adımı oluşturur/başlatır.
- Operation ID: `POST-/api/addresses`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `type` | integer (int32) | Evet |
| `title` | string | Evet |
| `firstName` | string | Evet |
| `lastName` | string | Evet |
| `phoneNumber` | string | Evet |
| `city` | string | Evet |
| `district` | string | Evet |
| `fullAddress` | string | Evet |
| `postalCode` | string | Hayır |
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
    "fullAddress":  "string",
    "postalCode":  "string",
    "isDefault":  true
}
```

## Başarılı response (200)

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
    "fullAddress":  "string",
    "postalCode":  "string",
    "isDefault":  true,
    "createdAt":  "2026-07-29T12:00:00Z",
    "updatedAt":  "2026-07-29T12:00:00Z"
}
```

