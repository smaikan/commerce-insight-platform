# GET /api/products/seo-index

- Görev alanı: **Katalog → Ürünler → Storefront**.

Arama motoru sitemap üretimi için yayındaki ürün URL'lerini ve son değişiklik zamanlarını sayfalı döndürür.

## Yetki

**Public.** Bearer token gerekmez.

## Query parametreleri

| Parametre | Tip | Varsayılan |
| --- | --- | ---: |
| `PageNumber` | integer | `1` |
| `PageSize` | integer | `100` |

```http
GET /api/products/seo-index?PageNumber=1&PageSize=100
```

## Başarılı response — 200 OK

```json
{
  "items": [
    {
      "url": "kirmizi-keten-gomlek",
      "lastModifiedAt": "2026-08-26T10:00:00Z"
    }
  ],
  "pageNumber": 1,
  "pageSize": 100,
  "totalCount": 1,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

## Hatalar

- `400`: sayfa değerleri geçersiz

> Güncel OpenAPI response metadata'sı bu endpointin body şemasını açık göstermese de controller/Application dönüşü `PagedResult<ProductSeoIndexItemDto>` biçimindedir.

