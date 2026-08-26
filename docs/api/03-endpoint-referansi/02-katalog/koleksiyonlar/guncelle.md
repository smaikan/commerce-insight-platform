# PUT /api/collections/{id}

- Görev alanı: **Katalog → Koleksiyonlar**.
- İşlev: günceller.
- Operation ID: `PUT-/api/collections/{id}`
- Yetki: **Admin**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `id` | path | Evet | string (uuid) |

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `name` | string | Evet |
| `url` | string | Hayır |
| `description` | string | Hayır |
| `displayOrder` | integer (int32) | Evet |
| `imageUrl` | string | Hayır |

```json
{
  "name": "string",
  "url": "string",
  "description": "string",
  "displayOrder": 1,
  "imageUrl": "https://cdn.example.com/collections/yaz.jpg"
}
```

## Başarılı response (200)

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "name": "string",
  "description": "string",
  "url": "string",
  "isActive": true,
  "isFeatured": true,
  "displayOrder": 1,
  "imageUrl": "https://cdn.example.com/collections/yaz.jpg"
}
```

