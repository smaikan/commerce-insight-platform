# POST /api/product-variants/by-product/{productId}

- Görev alanı: **Katalog → Varyantlar**.
- İşlev: Ürüne varyant ekler.
- Operation ID: `POST-/api/product-variants/by-product/{productId}`
- Yetki: **Admin**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `productId` | path | Evet | string |

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `name` | string | Evet |
| `value` | string | Evet |
| `sku` | string | Evet |
| `price` | number (double) | Evet |
| `stock` | integer (int32) | Evet |
| `compareAtPrice` | number (double) | Hayır |
| `barcode` | string | Hayır |
| `material` | string | Hayır |
| `isActive` | boolean | Evet |

```json
{
  "name": "string",
  "value": "string",
  "sku": "string",
  "price": 1,
  "stock": 1,
  "compareAtPrice": 1,
  "barcode": "string",
  "material": "string",
  "isActive": true
}
```

## Başarılı response (200)

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "productId": "string",
  "name": "string",
  "value": "string",
  "variantOptionNameId": "00000000-0000-0000-0000-000000000001",
  "variantOptionValueId": "00000000-0000-0000-0000-000000000001",
  "sku": "string",
  "barcode": "string",
  "material": "string",
  "price": 1,
  "netPrice": 1,
  "compareAtPrice": 1,
  "stock": 1,
  "addToCartCount": 1,
  "purchaseCount": 1,
  "isActive": true,
  "concurrencyToken": "00000000-0000-0000-0000-000000000004"
}
```
