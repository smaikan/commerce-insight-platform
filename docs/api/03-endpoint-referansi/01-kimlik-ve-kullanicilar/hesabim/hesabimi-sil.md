# DELETE /api/users/me

- Görev alanı: **Kimlik ve kullanıcılar → Hesabım**.
- İşlev: Hesabımı siler.
- Operation ID: `DELETE-/api/users/me`
- Yetki: **User**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `currentPassword` | string | Evet |

```json
{
  "currentPassword": "string"
}
```

## Başarılı response (200)

Response body yoktur.

