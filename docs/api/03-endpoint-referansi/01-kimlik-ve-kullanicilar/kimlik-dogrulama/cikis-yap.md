# POST /api/auth/logout

- Görev alanı: **Kimlik ve kullanıcılar → Kimlik doğrulama**.
- İşlev: Refresh token oturumunu geçersizleştirir.
- Operation ID: `POST-/api/auth/logout`
- Yetki: **Public**.
- Content-Type: request body varsa `application/json` gönderin.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `refreshToken` | string | Evet |

```json
{
  "refreshToken": "string"
}
```

## Başarılı response (204)

Response body yoktur.

Geçersiz request biçimi `400 ValidationProblemDetails` döner. İstemci, upstream logout sonucu ne olursa olsun kendi HttpOnly auth cookie'lerini temizlemelidir.
