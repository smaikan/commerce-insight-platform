# Alış Faturaları ve Giderler

`PurchaseInvoice` fiziksel stok hareketi oluşturmaz. UI, önce mevcut pozitif Purchase StockMovement kayıtlarını satırlara tahsis eder; posting sonrasında supplier debt ve FIFO CostLayer oluşur.

## Alış faturası endpointleri

| İşlem | Endpoint |
| --- | --- |
| Oluştur | `POST /api/accounting/purchase-invoices` |
| Toplu güncelle | `PUT /api/accounting/purchase-invoices/{id}` |
| Satır ekle | `POST /api/accounting/purchase-invoices/{id}/lines` |
| Satır güncelle | `PUT /api/accounting/purchase-invoices/{id}/lines/{lineId}` |
| Satır sil | `DELETE /api/accounting/purchase-invoices/{id}/lines/{lineId}` |
| Hareket tahsisi | `PUT /api/accounting/purchase-invoices/{id}/lines/{lineId}/allocations` |
| Post | `POST /api/accounting/purchase-invoices/{id}/post` |
| İptal | `POST /api/accounting/purchase-invoices/{id}/cancel` |
| Detay/liste | `GET /api/accounting/purchase-invoices/{id}`, `GET /api/accounting/purchase-invoices` |
| Tahsise açık hareketler | `GET /api/accounting/purchase-invoices/available-stock-movements?productVariantId={guid}` |

## Oluşturma örneği

```json
{
  "header": {
    "currentAccountId": "supplier-guid",
    "invoiceNumber": "ALIŞ-2026-001",
    "invoiceDate": "2026-07-27T00:00:00Z",
    "dueDate": "2026-08-27T00:00:00Z",
    "currencyCode": "TRY",
    "exchangeRate": 1,
    "description": "Temmuz alış faturası"
  },
  "lines": [
    {
      "lineNumber": 1,
      "productVariantId": "variant-guid",
      "purchaseQuantity": 10,
      "unitOfMeasure": "Adet",
      "unitsPerPurchaseUnit": 1,
      "priceEntryMode": 1,
      "vatRate": 20,
      "enteredUnitPrice": 100,
      "isInvoiceDiscountEligible": true
    }
  ]
}
```

`purchaseQuantity * unitsPerPurchaseUnit` tam sayı stok miktarı üretmelidir. İndirim alanları `01-genel-kurallar.md` içindeki enumlara göre opsiyoneldir.

## StockMovement tahsisi

Önce uygun hareketleri alın:

```http
GET /api/accounting/purchase-invoices/available-stock-movements?productVariantId={variantId}
```

Sonra ilgili fatura satırına toplam miktarı karşılayacak tahsis gönderin:

```json
[
  { "stockMovementId": "movement-guid-1", "quantity": 6 },
  { "stockMovementId": "movement-guid-2", "quantity": 4 }
]
```

Posting için her satın alma satırının tamamen tahsis edilmesi gerekir. Bir hareketin tahsis toplamı fiziksel miktarını geçemez.

## Post ve iptal

Post body gerektirmez. Taslak fatura post edildiğinde supplier debt ve maliyet katmanları oluşturur; StockMovement oluşturmaz.

İptal body:

```json
{ "reason": "Tedarikçi faturası iptal edildi." }
```

Tüketilmemiş maliyet katmanları geçersizleşir ve supplier debt için ters kayıt oluşur. Tüketilmiş katman varsa retroaktif maliyet politikası olmadan işlem engellenebilir; UI 409 mesajını göstermelidir.

## Gider kategorileri ve genel gider

| İşlem | Endpoint |
| --- | --- |
| Kategori oluştur | `POST /api/accounting/expenses/categories` |
| Kategori listele | `GET /api/accounting/expenses/categories` |
| Genel gider oluştur | `POST /api/accounting/expenses` |
| Genel gider listele | `GET /api/accounting/expenses` |

Kategori body:

```json
{ "code": "KIRA", "name": "Kira Gideri" }
```

Genel gider body:

```json
{
  "categoryId": "category-guid",
  "amountExcludingVat": 15000,
  "vatRate": 20,
  "expenseDate": "2026-07-27T00:00:00Z",
  "description": "Temmuz ofis kirası"
}
```

Genel gider stok maliyetini otomatik değiştirmez.

## Alış faturası gideri

| İşlem | Endpoint |
| --- | --- |
| Gider ekle | `POST /api/accounting/purchase-invoices/{id}/expenses` |
| Giderleri getir | `GET /api/accounting/purchase-invoices/{id}/expenses` |

```json
{
  "categoryId": "category-guid",
  "allocationMethod": 1,
  "amountExcludingVat": 1000,
  "vatRate": 20,
  "description": "Nakliye",
  "manualAllocations": null
}
```

`allocationMethod`: `1 VatExclusiveLineAmount`, `2 Quantity`, `3 Manual`.

Manual dağıtımda her fatura satırı bir kez bulunmalı ve KDV hariç tutarların toplamı gider tutarına eşit olmalıdır:

```json
{
  "categoryId": "category-guid",
  "allocationMethod": 3,
  "amountExcludingVat": 1000,
  "vatRate": 20,
  "manualAllocations": [
    { "purchaseInvoiceLineId": "line-guid-1", "amountExcludingVat": 700 },
    { "purchaseInvoiceLineId": "line-guid-2", "amountExcludingVat": 300 }
  ]
}
```
