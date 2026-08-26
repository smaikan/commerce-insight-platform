# POST /api/tags/bulk

- Görev alanı: **Katalog → Etiketler**.
- İşlev: Toplu oluşturur.
- Operation ID: `POST-/api/tags/bulk`
- Yetki: **Admin**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `tags` | array | Evet |

```json
{
  "tags": [
    {
      "name": "string",
      "url": "string",
      "isActive": true
    }
  ]
}
```

## Başarılı response (200)

Response body yoktur.

