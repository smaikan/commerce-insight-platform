# POST /api/auth/register

- Görev alanı: **Kimlik ve kullanıcılar → Kimlik doğrulama**.
- İşlev: Yeni müşteri hesabı oluşturur.
- Operation ID: `POST-/api/auth/register`
- Yetki: **Public**.
- Content-Type: request body varsa `application/json` gönderin.
- Rate limit: çözümlenmiş upstream IP başına dakikada 5 istek; login ve refresh kovalarından bağımsızdır.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `email` | string | Evet |
| `password` | string | Evet |
| `firstName` | string | Evet |
| `lastName` | string | Evet |
| `phoneNumber` | string | Hayır |

```json
{
  "email": "string",
  "password": "string",
  "firstName": "string",
  "lastName": "string",
  "phoneNumber": "string"
}
```

## Başarılı response (201)

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
  }
}
```

## Hata sözleşmesi

| HTTP | Gövde | Davranış |
| --- | --- | --- |
| `400` | `ValidationProblemDetails` | Kayıt alanları geçersizdir. |
| `409` | `ProblemDetails` | E-posta veya domain kuralı çakışmıştır. |
| `429` | `ProblemDetails`, `code=rate_limit_exceeded` | `Retry-After` kadar bekle; hızlı otomatik retry yapma. |
