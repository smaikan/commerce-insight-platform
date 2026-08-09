# Muhasebe Raporları

Tüm rapor endpointleri salt okunurdur ve `PagedResult<AccountingReportRowDto>` döner. Ortak query parametreleri:

```text
pageNumber=1&pageSize=20&from=2026-07-01T00:00:00Z&to=2026-07-31T23:59:59Z&search=ABC&hasSalesInvoice=true
```

`hasSalesInvoice` yalnız ilgili satış raporlarında kullanılmalıdır. Faturasız satışların görünmesi için bu parametreyi göndermeyin veya `false` kullanın.

## Ortak satır biçimi

```json
{
  "id": "guid",
  "relatedId": "guid-or-null",
  "number": "SAT-2026-001",
  "name": "Örnek Müşteri A.Ş.",
  "date": "2026-07-27T00:00:00Z",
  "dueDate": "2026-08-10T00:00:00Z",
  "amount": 1000,
  "secondaryAmount": 800,
  "tertiaryAmount": 200,
  "quantity": 2,
  "rate": 20,
  "hasSalesInvoice": false,
  "currencyCode": "TRY"
}
```

Alanların mali anlamı rapora göre değişir. Tablo başlığı, ilgili raporun UI sözleşmesinde açıkça tanımlanmalıdır; frontend ortak bir satırı otomatik finans tablosu gibi göstermemelidir.

## Endpoint kataloğu

| Grup | Endpoint |
| --- | --- |
| Satış listesi/detay/kalem | `/reports/sales`, `/reports/sales/{id}`, `/reports/sales/{id}/items` |
| Satış faturası listesi/detay | `/reports/sales-invoices`, `/reports/sales-invoices/{id}` |
| Alış faturası listesi/detay | `/reports/purchase-invoices`, `/reports/purchase-invoices/{id}` |
| Maliyetsiz/kısmi maliyetli hareket | `/reports/stock-movements/uncosted`, `/reports/stock-movements/partially-costed` |
| FIFO | `/reports/cost-layers`, `/reports/cost-layers/remaining`, `/reports/cost-layer-consumptions` |
| Varyant maliyet geçmişi | `/reports/product-variant-cost-history` |
| Stok değerleme | `/reports/warehouse-stock-valuation` |
| Kârlılık | `/reports/profitability/products`, `/reports/profitability/product-variants`, `/reports/profitability/sales-orders`, `/reports/profitability/sales-invoices` |
| Cari ekstre | `/reports/current-accounts/{id}/statement` |
| Alacak/borç | `/reports/receivables`, `/reports/debts`, `/reports/overdue-receivables`, `/reports/overdue-debts` |
| Ödeme/kasa/banka | `/reports/payments-and-collections`, `/reports/cash-movements`, `/reports/bank-movements` |
| KDV | `/reports/vat/purchases`, `/reports/vat/sales` |

## Frontend kullanım önerileri

- Satış ekranında ana kaynak `reports/sales` olmalıdır; `SalesInvoice` yoksa satır yine görünür.
- Kârlılıkta satış siparişi raporu gerçek FIFO maliyetini kullanır.
- `warehouse-stock-valuation` mevcut onaylı mimaride tek örtük stok alanını temsil eder; `warehouseId` filtresi yoktur.
- Vade listelerinde `dueDate` geçmişse satırı gecikmiş olarak vurgulayın.
- Büyük listelerde filtre değiştiğinde `pageNumber` değerini tekrar `1` yapın.
