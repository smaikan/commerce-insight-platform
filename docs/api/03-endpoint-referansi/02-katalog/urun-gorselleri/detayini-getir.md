# GET /api/product-images/{id}

- Görev alanı: **Katalog → Ürün görselleri**.
- İşlev: Detayını getirir.
- Operation ID: `GET-/api/product-images/{id}`
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
  "productId": "string",
  "imageUrl": "string",
  "altText": "string",
  "displayOrder": 1,
  "isMain": true
}
```

