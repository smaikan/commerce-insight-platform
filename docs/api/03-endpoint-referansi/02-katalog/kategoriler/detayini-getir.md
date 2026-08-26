# GET /api/product-types/{id}

- Görev alanı: **Katalog → Kategoriler**.
- İşlev: Detayını getirir.
- Operation ID: `GET-/api/product-types/{id}`
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
  "isActive": true,
  "imageUrl": "https://cdn.example.com/categories/category.webp"
}
```

`imageUrl`, kategoriye doğrudan atanmış nullable görseldir. Bu uç ürün görseli fallback'i hesaplamaz; storefront vitrin kartları `GET /api/product-types/published` ucunu kullanmalıdır.

