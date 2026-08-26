# GET /api/collections/{id}

- Görev alanı: **Katalog → Koleksiyonlar**.
- İşlev: Detayını getirir.
- Operation ID: `GET-/api/collections/{id}`
- Yetki: **Public**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `id` | path | Evet | string (uuid) |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

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

