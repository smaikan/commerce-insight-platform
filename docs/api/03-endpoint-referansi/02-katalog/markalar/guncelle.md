# PUT /api/brands/{id}

- Görev alanı: **Katalog → Markalar**.
- İşlev: günceller.
- Operation ID: `PUT-/api/brands/{id}`
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
| `imageUrl` | string veya null | Hayır |

```json
{
  "name": "string",
  "url": "string",
  "description": "string",
  "imageUrl": "https://cdn.example.com/brands/ornek-marka.png"
}
```

Bu endpoint tam güncelleme (`PUT`) uygular. `imageUrl` atlanır, `null` veya boş gönderilirse mevcut marka görseli kaldırılır; değer en fazla 500 karakterdir.

## Başarılı response (200)

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

