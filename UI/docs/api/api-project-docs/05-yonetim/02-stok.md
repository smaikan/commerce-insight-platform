# Stok ve StockMovement API'leri

StockMovement projenin tek fiziksel stok ledger'ıdır. ProductVariant.Stock yalnız hızlı okuma cache'idir; frontend hiçbir zaman doğrudan stock update yapmaz.

## Endpointler

| Method | Endpoint | Yetki | Amaç |
| --- | --- | --- | --- |
| POST | `/api/product-variants/stock-movements` | Admin | SKU ile tek imzalı hareket |
| POST | `/api/stock-movements/bulk` | Admin | Atomic bulk hareket |
| GET | `/api/stock-movements?pageNumber=1&pageSize=20&search=...&productVariantId=...&direction=...&type=...&createdFromUtc=...&createdToUtc=...` | Admin | Ürün/varyant/SKU aramalı hareket listesi |
| GET | `/api/stock-movements/variants/{variantId}/balance` | Admin | Cache/ledger karşılaştırması |

Tek hareket body:

```json
{ "productVariantSku": "TSHIRT-BLACK-M", "quantityDelta": 5, "type": 10, "reason": "Depo girişi" }
```

Bulk body:

```json
{
  "movements": [
    { "productVariantSku": "TSHIRT-BLACK-M", "quantityDelta": 5, "type": 10, "reason": "Alış" },
    { "productVariantSku": "TSHIRT-BLACK-L", "quantityDelta": -1, "type": 41, "reason": "Hasar" }
  ]
}
```

`productVariantSku` zorunludur, baştaki/sondaki boşluklar temizlenir ve en fazla 100 karakter olabilir. Yalnız aktif, silinmemiş ürünün aktif kaydı SKU üzerinden eşleştirilir; bulunamayan bir SKU `404 resource_not_found` üretir. `QuantityDelta` signed'dır; sıfır olamaz. Admin manuel olarak workflow-owned OpeningBalance, Sale, AccountingSale, SaleReturn ve Cancellation tiplerini kullanamaz. Bulk en fazla 500 satırdır ve satırların tümü başarılı olmazsa hiçbir stok hareketi kaydedilmez.

Stok hareketi listesi her satırda `id`, `productVariantId`, `productTitle`, `variantName`, `variantValue`, `sku`, `direction`, `type`, `quantityDelta`, `stockBeforeMovement`, `stockAfterMovement`, `reason`, `orderId`, `returnRequestId`, `createdAt` alanlarını döner. `search`, ürün başlığı, varyant adı/değeri veya SKU üzerinde çalışır.
