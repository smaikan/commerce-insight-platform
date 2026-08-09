# PUT /api/users/me/password

- İşlev alanı: **02 Kullanıcı ve adres**
- İşlev: Kaynağın güncellenebilir alanlarını değiştirir.
- Operation ID: `PUT-/api/users/me/password`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `currentPassword` | string | Evet |
| `newPassword` | string | Evet |

```json
{
    "currentPassword":  "string",
    "newPassword":  "string"
}
```

## Başarılı response (200)

Response body yoktur.

