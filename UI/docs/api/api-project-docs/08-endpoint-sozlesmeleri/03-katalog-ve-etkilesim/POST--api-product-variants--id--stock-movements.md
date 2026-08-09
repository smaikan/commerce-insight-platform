# POST /api/product-variants/{id}/stock-movements

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Yeni kaynak veya iş akışı adımı oluşturur/başlatır.
- Operation ID: `POST-/api/product-variants/{id}/stock-movements`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `id` | path | Evet | string (uuid) |

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `quantityDelta` | integer (int32) | Evet |
| `type` | integer (int32) | Evet |
| `reason` | string | Hayır |

```json
{
    "quantityDelta":  1,
    "type":  1,
    "reason":  "string"
}
```

## Başarılı response (200)

```json
{
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
}
```

