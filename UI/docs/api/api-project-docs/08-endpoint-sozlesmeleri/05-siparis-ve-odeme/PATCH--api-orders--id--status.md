# PATCH /api/orders/{id}/status

Ürün medya snapshot'larý ve kargo takip alanlarýnýn ortak açýklamasý: [OrderDto ürün medya ve kargo takip sözleþmesi](ORDER-DTO-VE-KARGO-TAKIP-SOZLESMESI.md).

- Ýþlev alaný: **05 Sipariþ ve ödeme**
- Ýþlev: Kaynaðýn belirli durum veya alanlarýný deðiþtirir.
- Operation ID: `PATCH-/api/orders/{id}/status`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Þema |
| --- | --- | --- | --- |
| `id` | path | Evet | string (uuid) |

## Request body

Aþaðýdaki örnek alan adlarýný camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `status` | integer (int32) | Evet |
| `shippingCarrier` | string, max 150 | Yalnýz `status=Shipped` için evet |
| `trackingNumber` | string, max 100 | Yalnýz `status=Shipped` için evet |
| `trackingUrl` | string, URI, max 500 | Hayýr |

```json
{
    "status":  4,
    "shippingCarrier": "Yurtiçi Kargo",
    "trackingNumber": "1234567890",
    "trackingUrl": "https://www.example-cargo.test/track/1234567890"
}
```

`status=Shipped` isteði kargo bilgisini atomik saklar ve `shippedAt` deðerini API üretir. `trackingUrl` doluysa mutlak HTTP/HTTPS olmalýdýr. `Delivered` geçiþinde `deliveredAt` yine API tarafýndan üretilir.

## Baþarýlý response (200)

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
    "neighborhood":  "string",
    "neighborhood":  "string",
                            "fullAddress":  "string",
                            "postalCode":  "string"
                        },
    "reservationExpiresAt":  "2026-07-29T12:00:00Z",
    "paidAt":  "2026-07-29T12:00:00Z",
    "cancelledAt":  "2026-07-29T12:00:00Z",
    "createdAt":  "2026-07-29T12:00:00Z"
}
```




