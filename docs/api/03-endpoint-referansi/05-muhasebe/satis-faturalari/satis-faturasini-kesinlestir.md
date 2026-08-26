# POST /api/accounting/sales-invoices/{id}/post

- Görev alanı: **Muhasebe → Satış faturaları**.
- İşlev: Taslak satış faturasını kesinleştirir ve ilgili kalıcı muhasebe etkilerini uygular.
- Operation ID: `POST-/api/accounting/sales-invoices/{id}/post`
- Yetki: **Admin**.
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
  "id": "00000000-0000-0000-0000-000000000001",
  "accountingSalesOrderId": "00000000-0000-0000-0000-000000000001",
  "currentAccountId": "00000000-0000-0000-0000-000000000001",
  "currentAccountName": "string",
  "taxNumberSnapshot": "string",
  "taxOfficeSnapshot": "string",
  "phoneNumberSnapshot": "string",
  "emailSnapshot": "string",
  "addressSnapshot": "string",
  "invoiceNumber": "string",
  "invoiceDate": "2026-07-29T12:00:00Z",
  "dueDate": "2026-07-29T12:00:00Z",
  "currencyCode": "string",
  "exchangeRate": 1,
  "status": 1,
  "description": "string",
  "invoiceDiscountType": 1,
  "invoiceDiscountValue": 1,
  "invoiceDiscountTaxBasis": 1,
  "subtotalExcludingVat": 1,
  "subtotalIncludingVat": 1,
  "lineDiscountTotalExcludingVat": 1,
  "lineDiscountTotalIncludingVat": 1,
  "invoiceDiscountTotalExcludingVat": 1,
  "invoiceDiscountTotalIncludingVat": 1,
  "totalDiscountExcludingVat": 1,
  "totalDiscountIncludingVat": 1,
  "netAmountExcludingVat": 1,
  "shippingTotal": 1,
  "shippingPayer": 0,
  "vatTotal": 1,
  "grandTotalIncludingVat": 1,
  "paidAmount": 1,
  "remainingAmount": 1,
  "totalCostOfGoodsSold": 1,
  "grossProfitExcludingVat": 1,
  "grossProfitMargin": 1,
  "createdAt": "2026-07-29T12:00:00Z",
  "postedAt": "2026-07-29T12:00:00Z",
  "cancelledBy": 1,
  "cancelledAt": "2026-07-29T12:00:00Z",
  "cancellationReason": "string",
  "lines": [
    {
      "id": "00000000-0000-0000-0000-000000000001",
      "accountingSalesOrderItemId": "00000000-0000-0000-0000-000000000001",
      "lineNumber": 1,
      "productId": "string",
      "productVariantId": "00000000-0000-0000-0000-000000000001",
      "productName": "string",
      "variantName": "string",
      "sku": "string",
      "barcode": "string",
      "quantity": 1,
      "unitOfMeasure": "string",
      "unitsPerSaleUnit": 1,
      "stockQuantity": 1,
      "enteredUnitPrice": 1,
      "priceEntryMode": 1,
      "unitPriceExcludingVat": 1,
      "unitPriceIncludingVat": 1,
      "vatRate": 1,
      "lineDiscountType": 1,
      "lineDiscountValue": 1,
      "lineDiscountTaxBasis": 1,
      "lineDiscountUnitBasis": 1,
      "isInvoiceDiscountEligible": true,
      "grossAmountExcludingVat": 1,
      "grossAmountIncludingVat": 1,
      "lineDiscountAmountExcludingVat": 1,
      "lineDiscountAmountIncludingVat": 1,
      "invoiceDiscountShareExcludingVat": 1,
      "invoiceDiscountShareIncludingVat": 1,
      "totalDiscountAmountExcludingVat": 1,
      "totalDiscountAmountIncludingVat": 1,
      "netAmountExcludingVat": 1,
      "vatAmount": 1,
      "totalAmountIncludingVat": 1,
      "costOfGoodsSold": 1,
      "grossProfitExcludingVat": 1,
      "grossProfitMargin": 1,
      "costLayerConsumptions": [
        {
          "id": "00000000-0000-0000-0000-000000000001",
          "inventoryCostLayerId": "00000000-0000-0000-0000-000000000001",
          "stockMovementId": "00000000-0000-0000-0000-000000000001",
          "quantity": 1,
          "unitCostExcludingVat": 1,
          "totalCostExcludingVat": 1,
          "createdAt": "2026-07-29T12:00:00Z"
        }
      ]
    }
  ]
}
```
