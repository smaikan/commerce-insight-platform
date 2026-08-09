# GET /api/orders/{id}

- İşlev alanı: **05 Sipariş ve ödeme**
- İşlev: Kaynağı veya filtrelenmiş kaynak listesini okur.
- Operation ID: `GET-/api/orders/{id}`
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
    "orderNumber":  "string",
    "status":  0,
    "subTotal":  1,
    "discountTotal":  1,
    "shippingTotal":  1,
    "taxTotal":  1,
    "grandTotal":  1,
    "couponCode":  "string",
    "shippingMethodName":  "string",
    "items":  {
                  "id":  "00000000-0000-0000-0000-000000000001",
                  "productId":  "string",
                  "productVariantId":  "00000000-0000-0000-0000-000000000001",
                  "productTitle":  "string",
                  "variantSku":  "string",
                  "unitPrice":  1,
                  "quantity":  1,
                  "totalPrice":  1,
                  "discountTotal":  1,
                  "taxRatePercentage":  1,
                  "taxTotal":  1,
                  "refundTotal":  1
              },
    "payments":  {
                     "id":  "00000000-0000-0000-0000-000000000001",
                     "provider":  0,
                     "status":  0,
                     "amount":  1,
                     "transactionId":  "string",
                     "paidAt":  "2026-07-29T12:00:00Z",
                     "createdAt":  "2026-07-29T12:00:00Z"
                 },
    "shippingAddress":  {
                            "sourceAddressId":  "00000000-0000-0000-0000-000000000001",
                            "title":  "string",
                            "firstName":  "string",
                            "lastName":  "string",
                            "phoneNumber":  "string",
                            "city":  "string",
                            "district":  "string",
                            "fullAddress":  "string",
                            "postalCode":  "string"
                        },
    "reservationExpiresAt":  "2026-07-29T12:00:00Z",
    "paidAt":  "2026-07-29T12:00:00Z",
    "cancelledAt":  "2026-07-29T12:00:00Z",
    "createdAt":  "2026-07-29T12:00:00Z"
}
```

