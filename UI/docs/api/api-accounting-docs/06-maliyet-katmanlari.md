# FIFO Maliyet Katmanları

Bu API yüzeyi ağırlıklı olarak görüntüleme ve açılış maliyetinin kontrollü güncellenmesi içindir. Satış posting'i FIFO tüketimini otomatik oluşturur.

## OpeningBalance maliyet katmanı

| İşlem | Endpoint |
| --- | --- |
| Varyanta göre açılış katmanını getir | `GET /api/accounting/inventory-cost-layers/opening-balance/by-variant/{productVariantId}` |
| Kalan açılış maliyetini güncelle | `PATCH /api/accounting/inventory-cost-layers/{id}/opening-balance-cost` |

Güncelleme body:

```json
{
  "expectedConcurrencyToken": "0f8fad5b-d9cb-469f-a165-70867728950e",
  "unitCostExcludingVat": 75.25,
  "unitCostIncludingVat": 90.30
}
```

`expectedConcurrencyToken`, GET response'undaki `concurrencyToken` değeridir. Başka bir kullanıcı katmanı değiştirdiyse API `409` döner; frontend veriyi yenileyip kullanıcıdan tekrar onay almalıdır.

Yalnız kalan OpeningBalance miktarının gelecekteki maliyeti değişir. Tüketilmiş CostLayer geçmişi sessizce yeniden yazılmaz.

## Varyant maliyet geçmişi

```http
GET /api/accounting/product-variants/{productVariantId}/cost-history
```

Örnek kayıt:

```json
{
  "id": "history-guid",
  "productVariantId": "variant-guid",
  "sourceType": 1,
  "sourceId": "purchase-invoice-guid",
  "previousCostExcludingVat": 70,
  "newCostExcludingVat": 75.25,
  "previousCostIncludingVat": 84,
  "newCostIncludingVat": 90.30,
  "validFrom": "2026-07-27T00:00:00Z",
  "validTo": null,
  "openingStockQuantity": 20,
  "closingStockQuantity": null,
  "createdAt": "2026-07-27T10:00:00Z"
}
```

`sourceType`: `1 PurchaseInvoice`, `2 OpeningBalance`.

FIFO detayları için rapor endpointlerini kullanın:

- `GET /api/accounting/reports/cost-layers`
- `GET /api/accounting/reports/cost-layers/remaining`
- `GET /api/accounting/reports/cost-layer-consumptions`
- `GET /api/accounting/reports/product-variant-cost-history`
