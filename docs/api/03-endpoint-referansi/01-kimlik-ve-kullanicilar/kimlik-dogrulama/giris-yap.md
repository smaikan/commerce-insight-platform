# POST /api/auth/login

- Görev alanı: **Kimlik ve kullanıcılar → Kimlik doğrulama**.
- İşlev: Kimlik doğrular; access ve refresh token döndürür.
- Operation ID: `POST-/api/auth/login`
- Yetki: **Public**.
- Content-Type: request body varsa `application/json` gönderin.
- Rate limit: çözümlenmiş upstream IP başına dakikada 5 istek; register ve refresh kovalarından bağımsızdır.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `email` | string | Evet |
| `password` | string | Evet |
| `deviceName` | string | Hayır |

```json
{
  "email": "string",
  "password": "string",
  "deviceName": "string"
}
```

## Hata sözleşmesi

| HTTP | Gövde | Davranış |
| --- | --- | --- |
| `400` | `ValidationProblemDetails` | E-posta/parola biçimi geçersizdir. |
| `401` | `ProblemDetails` | Kimlik bilgileri doğrulanamadı; hesap varlığı açıklanmaz. |
| `403` | `ProblemDetails` | Hesap veya rol erişime uygun değildir. |
| `429` | `ProblemDetails`, `code=rate_limit_exceeded` | `Retry-After` kadar bekle; hızlı otomatik retry yapma. |

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
