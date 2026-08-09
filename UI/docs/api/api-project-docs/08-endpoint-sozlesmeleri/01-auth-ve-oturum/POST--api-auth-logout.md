# POST /api/auth/logout

- İşlev alanı: **01 Auth ve oturum**
- İşlev: Refresh token oturumunu geçersizleştirir.
- Operation ID: `POST-/api/auth/logout`
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

```json
{
    "refreshToken":  "string"
}
```

## Başarılı response (200)

Response body yoktur.

