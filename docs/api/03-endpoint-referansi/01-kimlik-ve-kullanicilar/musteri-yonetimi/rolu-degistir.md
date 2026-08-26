# PATCH /api/users/{id}/role

- Görev alanı: **Kimlik ve kullanıcılar → Müşteri yönetimi**.
- İşlev: Rolü değiştirir.
- Operation ID: `PATCH-/api/users/{id}/role`
- Yetki: **Admin**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `id` | path | Evet | string |

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `role` | integer (int32) | Evet |

```json
{
  "role": 1
}
```

## Başarılı response (200)

```json
{
  "id": "string",
  "email": "string",
  "firstName": "string",
  "lastName": "string",
  "phoneNumber": "string",
  "role": 1,
  "status": 1,
  "lastLoginAt": "2026-07-29T12:00:00Z",
  "passwordChangedAt": "2026-07-29T12:00:00Z",
  "createdAt": "2026-07-29T12:00:00Z",
  "updatedAt": "2026-07-29T12:00:00Z"
}
```

