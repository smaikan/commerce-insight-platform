# POST /api/auth/refresh-token

- Görev alanı: **Kimlik ve kullanıcılar → Kimlik doğrulama**.
- İşlev: Refresh token ile yeni oturum token çiftini üretir.
- Operation ID: `POST-/api/auth/refresh-token`
- Yetki: **Public**.
- Content-Type: request body varsa `application/json` gönderin.
- Rate limit: çözümlenmiş upstream IP başına dakikada 120 istek; login ve register kovalarından bağımsızdır.

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
  "refreshToken": "string",
  "deviceName": "string"
}
```

Response yeni bir token çifti üretir ve önceki refresh tokenı döndürür. Aynı tokenla paralel refresh çağrıları tek uçuşta birleştirilmelidir.

## Hata sözleşmesi

| HTTP | Gövde | İstemci davranışı |
| --- | --- | --- |
| `400` | `ValidationProblemDetails` | Request biçimini düzelt. |
| `401` | `ProblemDetails` | Refresh token kesin geçersizdir; yerel oturumu temizle. |
| `429` | `ProblemDetails`, `code=rate_limit_exceeded` | Refresh tokenı koru, `Retry-After` kadar bekle ve hızlı retry döngüsü kurma. |

Geçici `5xx` cevaplarında da kullanılabilir refresh token silinmemeli; kontrollü yeniden deneme korunmalıdır.

## Başarılı response (200)

```json
{
  "user": {
    "id": "string",
    "email": "string",
    "firstName": "string",
    "lastName": "string",
    "phoneNumber": "string",
    "role": 1,
    "status": 1,
    "lastLoginAt": "2026-07-29T12:00:00Z",
    "createdAt": "2026-07-29T12:00:00Z",
    "updatedAt": "2026-07-29T12:00:00Z"
  },
  "tokens": {
    "accessToken": "string",
    "accessTokenExpiresAt": "2026-07-29T12:00:00Z",
    "refreshToken": "string",
    "refreshTokenExpiresAt": "2026-07-29T12:00:00Z"
  }
}
```
