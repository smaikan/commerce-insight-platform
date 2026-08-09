# PUT /api/products/{id}

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Kaynağın güncellenebilir alanlarını değiştirir.
- Operation ID: `PUT-/api/products/{id}`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `id` | path | Evet | string |

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `title` | string | Evet |
| `mainSku` | string | Evet |
| `type` | string | Hayır |
| `url` | string | Hayır |
| `brandId` | string (uuid) | Hayır |
| `description` | string | Hayır |
| `displayOrder` | integer (int32) | Evet |
| `seoTitle` | string | Hayır |
| `seoDescription` | string | Hayır |
| `tags` | array | Hayır |
| `taxRateId` | string (uuid) | Hayır |

```json
{
    "title":  "string",
    "mainSku":  "string",
    "type": "Giyim",
    "url":  "string",
    "brandId":  "00000000-0000-0000-0000-000000000001",
    "description":  "string",
    "displayOrder":  1,
    "seoTitle":  "string",
    "seoDescription":  "string",
    "tags":  "string",
    "taxRateId":  "00000000-0000-0000-0000-000000000001"
}
```

`type` isimle gönderilir; mevcut kayıt kullanılır, yoksa API oluşturur. `hasVariants` bu PUT gövdesinde değişmez; yalnız ilgili PATCH endpoint’i kullanılır.

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
             }
}
```

