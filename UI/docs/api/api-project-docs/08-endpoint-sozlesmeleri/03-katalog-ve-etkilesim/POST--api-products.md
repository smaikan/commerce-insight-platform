# POST /api/products

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Yeni kaynak veya iş akışı adımı oluşturur/başlatır.
- Operation ID: `POST-/api/products`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `title` | string | Evet |
| `mainSku` | string | Evet |
| `hasVariants` | boolean | Hayır, varsayılan `false` |
| `type` | string | Hayır |
| `url` | string | Hayır |
| `brandId` | string (uuid) | Hayır |
| `description` | string | Hayır |
| `status` | integer (int32) | Evet |
| `isFeatured` | boolean | Evet |
| `displayOrder` | integer (int32) | Evet |
| `seoTitle` | string | Hayır |
| `seoDescription` | string | Hayır |
| `collections` | string array | Hayır |
| `variants` | array | Hayır |
| `tags` | array | Hayır |
| `taxRateId` | string (uuid) | Hayır |

```json
{
    "title":  "string",
    "mainSku":  "string",
    "hasVariants": false,
    "type": "Giyim",
    "url":  "string",
    "brandId":  "00000000-0000-0000-0000-000000000001",
    "description":  "string",
    "status":  0,
    "isFeatured":  true,
    "displayOrder":  1,
    "seoTitle":  "string",
    "seoDescription":  "string",
    "collections": ["Yaz Koleksiyonu"],
    "variants":  {
                     "name":  "string",
                     "value":  "string",
                     "sku":  "string",
                     "price":  1,
                     "stock":  1,
                     "compareAtPrice":  1,
                     "barcode":  "string",
                     "material":  "string",
                     "isActive":  true,
                     "openingUnitCostExcludingVat":  1,
                     "openingUnitCostIncludingVat":  1
                 },
    "tags":  "string",
    "taxRateId":  "00000000-0000-0000-0000-000000000001"
}
```

`type`, `collections` ve `tags` isimle gönderilir; eşleşen kayıt yeniden kullanılır, kayıt yoksa API oluşturur. Birden fazla `variants` öğesi gönderiliyorsa `hasVariants` değeri `true` olmalıdır.

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
             }
}
```

