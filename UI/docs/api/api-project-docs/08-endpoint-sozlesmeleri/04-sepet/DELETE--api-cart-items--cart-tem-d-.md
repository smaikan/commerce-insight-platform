# DELETE /api/cart/items/{cartItemId}

- İşlev alanı: **04 Sepet**
- İşlev: Kaynağı ya da ilişkisini kaldırır.
- Operation ID: `DELETE-/api/cart/items/{cartItemId}`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `cartItemId` | path | Evet | string (uuid) |
| `expectedConcurrencyToken` | query | Hayır | string (uuid) |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

```json
{
    "id":  "00000000-0000-0000-0000-000000000001",
    "concurrencyToken":  "00000000-0000-0000-0000-000000000001",
    "items":  {
                  "id":  "00000000-0000-0000-0000-000000000001",
                  "productId":  "string",
                  "productVariantId":  "00000000-0000-0000-0000-000000000001",
                  "productTitle":  "string",
                  "variantName":  "string",
                  "sku":  "string",
                  "quantity":  1,
                  "unitPrice":  1,
                  "currentUnitPrice":  1,
                  "totalPrice":  1,
                  "availableStock":  1,
                  "isAvailable":  true,
                  "priceChanged":  true,
                  "createdAt":  "2026-07-29T12:00:00Z"
              },
    "totalQuantity":  1,
    "subTotal":  1,
    "hasUnavailableItems":  true,
    "hasPriceChanges":  true,
    "createdAt":  "2026-07-29T12:00:00Z",
    "updatedAt":  "2026-07-29T12:00:00Z"
}
```

