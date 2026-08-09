# POST /api/auth/refresh-token

- İşlev alanı: **01 Auth ve oturum**
- İşlev: Refresh token ile yeni oturum token çiftini üretir.
- Operation ID: `POST-/api/auth/refresh-token`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `refreshToken` | string | Evet |
| `deviceName` | string | Hayır |

```json
{
    "refreshToken":  "string",
    "deviceName":  "string"
}
```

## Başarılı response (200)

```json
{
    "user":  {
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
             },
    "tokens":  {
                   "accessToken":  "string",
                   "accessTokenExpiresAt":  "2026-07-29T12:00:00Z",
                   "refreshToken":  "string",
                   "refreshTokenExpiresAt":  "2026-07-29T12:00:00Z"
               }
}
```

