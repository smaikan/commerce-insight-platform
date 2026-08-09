# Muhasebe API'leri

Accounting endpointlerinin tamamı `AdminOnly` yetkilidir. AccountingSalesOrder, mevcut e-commerce Order/Cart'tan bağımsızdır; `CurrentAccountId` ve doğrudan ProductVariant kullanır. SalesInvoice opsiyoneldir.

## Cari hesap

```text
POST /api/accounting/current-accounts
PUT  /api/accounting/current-accounts/{id}
GET  /api/accounting/current-accounts/{id}
GET  /api/accounting/current-accounts?pageNumber=1&pageSize=20
```

Create body:

```json
{ "code": "CARI-001", "type": 1, "name": "Müşteri A.Ş.", "taxNumber": "1111111111", "taxOffice": "Kadıköy", "city": "İstanbul", "addressLine": "..." }
```

Response `CurrentAccountDto`: id, code, type, name, trade/tax/identity, phone/email/adres alanları, isActive ve userId.

## AccountingSalesOrder

| Method | Endpoint | Body/amaç |
| --- | --- | --- |
| POST | `/api/accounting/sales-orders` | Header + lines + `createInvoice` |
| PUT | `/api/accounting/sales-orders/{id}` | Header + tam lines listesi |
| POST | `/api/accounting/sales-orders/{id}/items` | Tek item ekle |
| PUT | `/api/accounting/sales-orders/{id}/items/{itemId}` | Commercial item güncelle |
| DELETE | `/api/accounting/sales-orders/{id}/items/{itemId}` | Draft item kaldır |
| POST | `/api/accounting/sales-orders/{id}/post` | StockMovement AccountingSale + FIFO + cari receivable |
| POST | `/api/accounting/sales-orders/{id}/cancel` | `{ "reason": "..." }` ile reversal |
| GET | `/api/accounting/sales-orders/{id}` | Detay |
| GET | `/api/accounting/sales-orders?pageNumber=1&pageSize=20` | Özet liste |

Create body özeti:

```json
{
  "header": { "currentAccountId": "account-guid", "orderNumber": "ASO-001", "orderDate": "2026-07-27T00:00:00Z", "dueDate": null, "currencyCode": "TRY", "exchangeRate": 1, "shippingTotal": 0, "shippingPayer": 0, "description": null },
  "lines": [{ "lineNumber": 1, "productVariantId": "variant-guid", "quantity": 2, "unitOfMeasure": "Adet", "unitsPerSaleUnit": 1, "priceEntryMode": 1, "vatRate": 20, "enteredUnitPrice": 250, "isInvoiceDiscountEligible": true }],
  "createInvoice": false,
  "invoice": null
}
```

`createInvoice=false` SalesInvoice oluşturmaz; `true` tam bir invoice header ister. Draft stok/cari etkilemez. `AccountingSalesOrderDto` header totals, paid/remaining, FIFO cost/profit, status, SalesInvoiceId ve item graph döner.

## SalesInvoice

| Method | Endpoint | Yetki/amaç |
| --- | --- | --- |
| POST | `/api/accounting/sales-invoices` | Direct invoice; aynı transaction'da tek AccountingSalesOrder |
| POST | `/api/accounting/sales-invoices/from-order/{accountingSalesOrderId}` | Sonradan invoice |
| PUT | `/api/accounting/sales-invoices/{id}` | Header + tam `lines` listesiyle genel draft update |
| POST/PUT/DELETE | `/api/accounting/sales-invoices/{id}/lines...` | Legacy tekil satır işlemleri |
| POST | `/api/accounting/sales-invoices/{id}/post` | Bağlı order posting'e yönlenir |
| POST | `/api/accounting/sales-invoices/{id}/cancel` | Invoice cancellation; fiziksel stok yaratmaz |
| GET | `/api/accounting/sales-invoices/{id}` | Detay |
| GET | `/api/accounting/sales-invoices?pageNumber=1&pageSize=20` | Özet liste |

Genel update body:

```json
{
  "header": { "invoiceNumber": "SF-001", "invoiceDate": "2026-07-27T00:00:00Z", "dueDate": null, "description": "Güncel" },
  "lines": [{ "lineNumber": 1, "productVariantId": "variant-guid", "quantity": 3, "unitOfMeasure": "Adet", "unitsPerSaleUnit": 1, "priceEntryMode": 1, "vatRate": 20, "enteredUnitPrice": 250, "isInvoiceDiscountEligible": true }]
}
```

Listede olmayan draft satırlar kaldırılır; Posted/Cancelled düzenlenemez. SalesInvoice doğrudan StockMovement veya ikinci cari alacak oluşturmaz.

## PurchaseInvoice ve gider

| Method | Endpoint | Amaç |
| --- | --- | --- |
| POST | `/api/accounting/purchase-invoices` | Draft alış fatura |
| PUT | `/api/accounting/purchase-invoices/{id}` | Header + tam lines |
| POST/PUT/DELETE | `/api/accounting/purchase-invoices/{id}/lines...` | Satır yönetimi |
| PUT | `/api/accounting/purchase-invoices/{id}/lines/{lineId}/allocations` | Purchase StockMovement allocation |
| POST | `/api/accounting/purchase-invoices/{id}/post` | SupplierDebt + FIFO layer; StockMovement oluşturmaz |
| POST | `/api/accounting/purchase-invoices/{id}/cancel` | reason ile cancellation |
| POST | `/api/accounting/purchase-invoices/{id}/expenses` | Purchase expense |
| GET | `/api/accounting/purchase-invoices/{id}/expenses` | Expense list |
| GET | `/api/accounting/purchase-invoices/{id}` | Detay |
| GET | `/api/accounting/purchase-invoices?pageNumber=1&pageSize=20` | Liste |
| GET | `/api/accounting/purchase-invoices/available-stock-movements?productVariantId=...` | Tahsise uygun Purchase hareketleri |

Purchase expense body:

```json
{ "categoryId": "category-guid", "allocationMethod": 1, "amountExcludingVat": 1000, "vatRate": 20, "description": "Nakliye", "manualAllocations": null }
```

Allocation method: `1 VatExclusiveLineAmount`, `2 Quantity`, `3 Manual`. General expense endpointleri `/api/accounting/expenses/categories` ve `/api/accounting/expenses` altındadır; yalnız create/list yaşam döngüsü vardır.

## Payment, tahsilat, kasa ve banka

| Method | Endpoint | Amaç |
| --- | --- | --- |
| POST | `/api/accounting/payments` | CustomerCollection/SupplierPayment |
| GET | `/api/accounting/payments/{id}` | Payment detail |
| GET | `/api/accounting/payments?pageNumber=1&pageSize=20` | Payment list |
| POST | `/api/accounting/payments/{id}/cancel` | Payment reversal |
| POST | `/api/accounting/cash-accounts` | Kasa oluştur |
| GET | `/api/accounting/cash-accounts` | Kasa seçim listesi |
| GET | `/api/accounting/cash-accounts/{id}/statement` | Kasa ekstresi |
| POST | `/api/accounting/bank-accounts` | Banka oluştur |
| GET | `/api/accounting/bank-accounts` | Banka seçim listesi |
| GET | `/api/accounting/bank-accounts/{id}/statement` | Banka ekstresi |
| POST | `/api/accounting/financial-transactions` | CashIn/CashOut/commission/refund |
| POST | `/api/accounting/financial-transactions/bank-transfers` | Atomic BankTransferOut + In |
| POST | `/api/accounting/financial-transactions/{id}/reverse` | Finans reversal |

Payment create body:

```json
{
  "currentAccountId": "supplier-guid",
  "type": 2,
  "amount": 250,
  "paymentDate": "2026-07-27T00:00:00Z",
  "cashAccountId": null,
  "bankAccountId": "bank-guid",
  "currencyCode": "TRY",
  "exchangeRate": 1,
  "allocations": []
}
```

SupplierPayment boş allocation ile faturasız tediye/avans oluşturabilir. CustomerCollection allocation ister. Payment response'ta `allocatedAmount` ve `unallocatedAmount` vardır.

## Maliyet ve raporlar

```text
GET   /api/accounting/inventory-cost-layers/opening-balance/by-variant/{productVariantId}
PATCH /api/accounting/inventory-cost-layers/{id}/opening-balance-cost
GET   /api/accounting/product-variants/{productVariantId}/cost-history
```

Rapor kökü `/api/accounting/reports` altındadır: sales, sales-items, sales-invoices, purchase-invoices, uncosted/partially-costed stock, cost-layers, remaining, cost-layer-consumptions, product-variant-cost-history, warehouse valuation, product/product-variant/order/invoice profitability, current account statement, receivables/debts/overdue, payments, cash/bank movements ve VAT purchases/sales.

Ortak query: `pageNumber`, `pageSize`, `from`, `to`, `id`, `search`, `hasSalesInvoice`. Response `AccountingReportRowDto` alanları: `id`, `relatedId`, `number`, `name`, `date`, `dueDate`, `amount`, `secondaryAmount`, `tertiaryAmount`, `quantity`, `rate`, `hasSalesInvoice`, `currencyCode`.
