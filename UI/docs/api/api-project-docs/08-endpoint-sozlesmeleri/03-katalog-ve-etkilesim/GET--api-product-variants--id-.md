# GET /api/product-variants/{id}

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Kaynağı veya filtrelenmiş kaynak listesini okur.
- Operation ID: `GET-/api/product-variants/{id}`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
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
    "isActive":  true,
    "concurrencyToken":  "00000000-0000-0000-0000-000000000004"
}
```
