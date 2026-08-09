# GET /api/accounting/purchase-invoices/{id}

- İşlev alanı: **10 Muhasebe alış ve gider**
- İşlev: Kaynağı veya filtrelenmiş kaynak listesini okur.
- Operation ID: `GET-/api/accounting/purchase-invoices/{id}`
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
    "currentAccountId":  "00000000-0000-0000-0000-000000000001",
    "currentAccountName":  "string",
    "taxNumberSnapshot":  "string",
    "taxOfficeSnapshot":  "string",
    "phoneNumberSnapshot":  "string",
    "emailSnapshot":  "string",
    "addressSnapshot":  "string",
    "invoiceNumber":  "string",
    "invoiceDate":  "2026-07-29T12:00:00Z",
    "dueDate":  "2026-07-29T12:00:00Z",
    "currencyCode":  "string",
    "exchangeRate":  1,
    "status":  1,
    "description":  "string",
    "subtotalExcludingVat":  1,
    "subtotalIncludingVat":  1,
    "lineDiscountTotalExcludingVat":  1,
    "lineDiscountTotalIncludingVat":  1,
    "invoiceDiscountTotalExcludingVat":  1,
    "invoiceDiscountTotalIncludingVat":  1,
    "totalDiscountExcludingVat":  1,
    "totalDiscountIncludingVat":  1,
    "netAmountExcludingVat":  1,
    "vatTotal":  1,
    "grandTotalIncludingVat":  1,
    "totalFinalCostExcludingVat":  1,
    "totalFinalCostIncludingVat":  1,
    "paidAmount":  1,
    "remainingAmount":  1,
    "createdAt":  "2026-07-29T12:00:00Z",
    "updatedAt":  "2026-07-29T12:00:00Z",
    "postedAt":  "2026-07-29T12:00:00Z",
    "cancelledBy":  1,
    "cancelledAt":  "2026-07-29T12:00:00Z",
    "cancellationReason":  "string",
    "lines":  {
                  "id":  "00000000-0000-0000-0000-000000000001",
                  "lineNumber":  1,
                  "productId":  "string",
                  "productVariantId":  "00000000-0000-0000-0000-000000000001",
                  "productName":  "string",
                  "variantName":  "string",
                  "sku":  "string",
                  "barcode":  "string",
                  "purchaseQuantity":  1,
                  "unitOfMeasure":  "string",
                  "unitsPerPurchaseUnit":  1,
                  "stockQuantity":  1,
                  "enteredUnitPrice":  1,
                  "priceEntryMode":  1,
                  "unitPriceExcludingVat":  1,
                  "unitPriceIncludingVat":  1,
                  "vatRate":  1,
                  "grossAmountExcludingVat":  1,
                  "grossAmountIncludingVat":  1,
                  "totalDiscountAmountExcludingVat":  1,
                  "totalDiscountAmountIncludingVat":  1,
                  "netAmountExcludingVat":  1,
                  "vatAmount":  1,
                  "totalAmountIncludingVat":  1,
                  "finalUnitCostExcludingVat":  1,
                  "finalUnitCostIncludingVat":  1,
                  "allocations":  {
                                      "id":  "00000000-0000-0000-0000-000000000001",
                                      "stockMovementId":  "00000000-0000-0000-0000-000000000001",
                                      "allocatedQuantity":  1
                                  }
              }
}
```

