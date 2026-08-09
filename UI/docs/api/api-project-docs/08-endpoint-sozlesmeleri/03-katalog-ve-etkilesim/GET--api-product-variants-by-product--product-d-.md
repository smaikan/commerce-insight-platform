# GET /api/product-variants/by-product/{productId}

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Kaynağı veya filtrelenmiş kaynak listesini okur.
- Operation ID: `GET-/api/product-variants/by-product/{productId}`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `productId` | path | Evet | string |
| `pageNumber` | query | Hayır | integer (int32) |
| `pageSize` | query | Hayır | integer (int32) |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

```json
{
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000001",
      "productId": "P00001",
      "name": "Ebat",
      "value": "100x150",
      "variantOptionNameId": "00000000-0000-0000-0000-000000000002",
      "variantOptionValueId": "00000000-0000-0000-0000-000000000003",
      "sku": "HALI-100X150",
      "price": 2500,
      "netPrice": 2083.33,
      "stock": 12,
      "isActive": true
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 1
}
```

