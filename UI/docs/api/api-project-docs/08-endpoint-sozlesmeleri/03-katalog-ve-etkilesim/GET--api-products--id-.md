# GET /api/products/{id}

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Kaynağı veya filtrelenmiş kaynak listesini okur.
- Operation ID: `GET-/api/products/{id}`
- Yetki: `AdminOnly`.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `id` | path | Evet | string |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

```json
{
    "id":  "string",
    "title":  "string",
    "mainSku":  "string",
    "description":  "string",
    "url":  "string",
    "typeId":  "00000000-0000-0000-0000-000000000001",
    "typeName":  "string",
    "brandId":  "00000000-0000-0000-0000-000000000001",
    "brandName":  "string",
    "taxRateId":  "00000000-0000-0000-0000-000000000001",
    "taxRateName":  "string",
    "taxRatePercentage":  1,
    "status":  0,
    "isFeatured":  true,
    "hasVariants":  true,
    "displayOrder":  1,
    "seoTitle":  "string",
    "seoDescription":  "string",
    "clickCount":  1,
    "totalAddToCartCount":  1,
    "totalPurchaseCount":  1,
    "favoriteCount":  1,
    "popularityScore":  1,
    "averageRating":  1,
    "ratingCount":  1,
    "reviewCount":  1,
    "variants":  {
                     "id":  "00000000-0000-0000-0000-000000000001",
                     "productId":  "string",
                     "name":  "string",
                     "value":  "string",
                     "variantOptionNameId":  "00000000-0000-0000-0000-000000000001",
                     "variantOptionValueId":  "00000000-0000-0000-0000-000000000001",
                     "sku":  "string",
                     "barcode":  "string",
                     "material":  "string",
                     "price":  1,
                     "netPrice":  1,
                     "compareAtPrice":  1,
                     "stock":  1,
                     "addToCartCount":  1,
                     "purchaseCount":  1,
                     "isActive":  true
                 },
    "tags":  {
                 "id":  "00000000-0000-0000-0000-000000000001",
                 "name":  "string",
                 "url":  "string",
                 "isActive":  true
             },
    "collections":  [{
                         "id":  "00000000-0000-0000-0000-000000000001",
                         "name":  "Yaz Koleksiyonu",
                         "url":  "yaz-koleksiyonu",
                         "isActive":  true,
                         "isFeatured":  false,
                         "displayOrder":  1
                     }],
    "images":  [{
                    "id":  "00000000-0000-0000-0000-000000000001",
                    "productId":  "P00001",
                    "imageUrl":  "https://cdn.example.com/product.jpg",
                    "altText":  "Ürün ön görünümü",
                    "displayOrder":  0,
                    "isMain":  true
                }],
    "summary":  "Kısa ürün açıklaması",
    "mainImage":  {
                      "id":  "00000000-0000-0000-0000-000000000001",
                      "productId":  "P00001",
                      "imageUrl":  "https://cdn.example.com/product.jpg",
                      "altText":  "Ürün ön görünümü",
                      "displayOrder":  0,
                      "isMain":  true
                  }
}
```

