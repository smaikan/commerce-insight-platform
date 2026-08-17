# GET /api/product-types

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Yönetim ve genel sınıflandırma kullanımı için bütün ürün türlerini sayfalı döndürür.
- Yetki: `AllowAnonymous`; OpenAPI operation `security: []`.
- Başarı: `200 PagedResult<ProductTypeDto>`.

## Query

| Alan | Zorunlu | Varsayılan | Kural |
| --- | --- | ---: | --- |
| `PageNumber` | Hayır | `1` | Pozitif integer. |
| `PageSize` | Hayır | `20` | `1..100`. |

`ProductTypeDto` alanları: `id`, `name`, nullable `description`, `isActive`, nullable `imageUrl`. Buradaki `imageUrl` yalnız ürün türüne kalıcı olarak atanmış özel görseldir; ürün fallback'i uygulanmaz. Storefront kategori kartları için `GET /api/product-types/published` kullanılmalıdır.

## Örnek 200

```json
{
  "items": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "name": "Ayakkabı",
      "description": "Ayakkabı ürünleri",
      "isActive": true,
      "imageUrl": "https://cdn.example.com/categories/shoes.webp"
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

Geçersiz sayfalama ortak `400 application/problem+json` cevabıdır.
