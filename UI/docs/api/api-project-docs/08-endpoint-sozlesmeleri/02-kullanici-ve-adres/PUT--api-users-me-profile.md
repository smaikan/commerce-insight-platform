# PUT /api/users/me/profile

- İşlev alanı: **02 Kullanıcı ve adres**
- İşlev: Kaynağın güncellenebilir alanlarını değiştirir.
- Operation ID: `PUT-/api/users/me/profile`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `firstName` | string | Evet |
| `lastName` | string | Evet |
| `phoneNumber` | string | Hayır |

```json
{
    "firstName":  "string",
    "lastName":  "string",
    "phoneNumber":  "string"
}
```

## Başarılı response (200)

```json
{
    "id":  "string",
    "email":  "string",
    "firstName":  "string",
    "lastName":  "string",
    "phoneNumber":  "string",
    "role":  1,
    "status":  1,
    "lastLoginAt":  "2026-07-29T12:00:00Z",
    "createdAt":  "2026-07-29T12:00:00Z",
    "updatedAt":  "2026-07-29T12:00:00Z"
}
```

