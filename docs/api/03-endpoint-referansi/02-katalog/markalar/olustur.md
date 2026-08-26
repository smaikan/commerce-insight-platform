# POST /api/brands

- Görev alanı: **Katalog → Markalar**.
- İşlev: oluşturur.
- Operation ID: `POST-/api/brands`
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
| `description` | string | Hayır |
| `isActive` | boolean | Evet |
| `imageUrl` | string veya null | Hayır |

```json
{
  "name": "string",
  "url": "string",
  "description": "string",
  "isActive": true,
  "imageUrl": "https://cdn.example.com/brands/ornek-marka.png"
}
```

`imageUrl` en fazla 500 karakterdir. Alan atlanır, `null` veya boş gönderilirse marka görselsiz oluşturulur.

## Başarılı response (201)

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "name": "string",
  "description": "string",
  "url": "string",
  "isActive": true,
  "imageUrl": "https://cdn.example.com/brands/ornek-marka.png"
}
```

