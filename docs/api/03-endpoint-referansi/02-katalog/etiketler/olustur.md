# POST /api/tags

- Görev alanı: **Katalog → Etiketler**.
- İşlev: oluşturur.
- Operation ID: `POST-/api/tags`
- Yetki: **Admin**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `name` | string | Evet |
| `url` | string | Hayır |
| `isActive` | boolean | Evet |

```json
{
  "name": "string",
  "url": "string",
  "isActive": true
}
```

## Başarılı response (200)

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "name": "string",
  "url": "string",
  "isActive": true
}
```

