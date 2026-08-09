# PUT /api/product-engagement/products/{productId}/rating

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Kaynağın güncellenebilir alanlarını değiştirir.
- Operation ID: `PUT-/api/product-engagement/products/{productId}/rating`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
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
| `ratingValue` | integer (int32) | Evet |

```json
{
    "ratingValue":  1
}
```

## Başarılı response (200)

Response body yoktur.

