# Stok ve StockMovement API'leri

StockMovement projenin tek fiziksel stok ledger'ıdır. ProductVariant.Stock yalnız hızlı okuma cache'idir; frontend hiçbir zaman doğrudan stock update yapmaz.

## Endpointler

| Method | Endpoint | Yetki | Amaç |
| --- | --- | --- | --- |
| POST | `/api/product-variants/{variantId}/stock-movements` | Admin | Tek imzalı hareket |
| POST | `/api/stock-movements/bulk` | Admin | Atomic bulk hareket |
| GET | `/api/stock-movements?pageNumber=1&pageSize=20&search=...&productVariantId=...&direction=...&type=...&createdFromUtc=...&createdToUtc=...` | Admin | Ürün/varyant/SKU aramalı hareket listesi |
| GET | `/api/stock-movements/variants/{variantId}/balance` | Admin | Cache/ledger karşılaştırması |

Tek hareket body:

```json
{ "quantityDelta": 5, "type": 10, "reason": "Depo girişi" }
```

Bulk body:

```json
{
  "movements": [
    { "productVariantId": "variant-guid", "quantityDelta": 5, "type": 10, "reason": "Alış" },
    { "productVariantId": "variant-guid-2", "quantityDelta": -1, "type": 40, "reason": "Hasar" }
  ]
}
```

`QuantityDelta` signed'dır; sıfır olamaz. Admin manuel olarak workflow-owned OpeningBalance, Sale, AccountingSale, SaleReturn ve Cancellation tiplerini kullanamaz. Bulk max 500 satırdır ve tümü başarılı ya da tümü rollback olur.

Stok hareketi listesi her satırda `id`, `productVariantId`, `productTitle`, `variantName`, `variantValue`, `sku`, `direction`, `type`, `quantityDelta`, `stockBeforeMovement`, `stockAfterMovement`, `reason`, `orderId`, `returnRequestId`, `createdAt` alanlarını döner. `search`, ürün başlığı, varyant adı/değeri veya SKU üzerinde çalışır.
