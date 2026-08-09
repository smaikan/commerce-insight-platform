# Accounting Satış Siparişleri ve Faturalar

`AccountingSalesOrder`, muhasebe satışının ana belgesidir. E-ticaret `Order`, kullanıcı veya sepet kullanılmaz. `SalesInvoice` opsiyoneldir ve aynı satışın ikinci stok/cari etkisini oluşturmaz.

## AccountingSalesOrder endpointleri

| İşlem | Endpoint |
| --- | --- |
| Oluştur | `POST /api/accounting/sales-orders` |
| Güncelle | `PUT /api/accounting/sales-orders/{id}` |
| Kalem ekle | `POST /api/accounting/sales-orders/{id}/items` |
| Kalem güncelle | `PUT /api/accounting/sales-orders/{id}/items/{itemId}` |
| Kalem sil | `DELETE /api/accounting/sales-orders/{id}/items/{itemId}` |
| Post | `POST /api/accounting/sales-orders/{id}/post` |
| İptal | `POST /api/accounting/sales-orders/{id}/cancel` |
| Detay/liste | `GET /api/accounting/sales-orders/{id}`, `GET /api/accounting/sales-orders` |

Oluştururken `Idempotency-Key` header zorunludur.

## Faturasız satış oluşturma

```http
POST /api/accounting/sales-orders
Idempotency-Key: 11111111-1111-1111-1111-111111111111
```

```json
{
  "header": {
    "currentAccountId": "customer-guid",
    "orderNumber": "SAT-2026-001",
    "orderDate": "2026-07-27T00:00:00Z",
    "dueDate": "2026-08-10T00:00:00Z",
    "currencyCode": "TRY",
    "exchangeRate": 1,
    "shippingTotal": 50,
    "shippingPayer": 2,
    "description": "Faturasız müşteri satışı"
  },
  "lines": [
    {
      "lineNumber": 1,
      "productVariantId": "variant-guid",
      "quantity": 2,
      "unitOfMeasure": "Adet",
      "unitsPerSaleUnit": 1,
      "priceEntryMode": 1,
      "vatRate": 20,
      "enteredUnitPrice": 250,
      "isInvoiceDiscountEligible": true
    }
  ],
  "createInvoice": false,
  "invoice": null
}
```

Taslak sipariş stok ve cari hesap etkilemez. `POST /{id}/post` çağrısı mevcut StockMovement sisteminde stok çıkışı, FIFO consumption ve müşteri alacağı oluşturur.

## Oluştururken fatura üretme

`createInvoice: true` seçildiğinde `invoice` zorunludur:

```json
{
  "header": { "currentAccountId": "customer-guid", "orderNumber": "SAT-2026-002", "orderDate": "2026-07-27T00:00:00Z" },
  "lines": [{ "lineNumber": 1, "productVariantId": "variant-guid", "quantity": 1, "unitOfMeasure": "Adet", "unitsPerSaleUnit": 1, "priceEntryMode": 1, "vatRate": 20, "enteredUnitPrice": 250 }],
  "createInvoice": true,
  "invoice": { "invoiceNumber": "SF-2026-001", "invoiceDate": "2026-07-27T00:00:00Z", "dueDate": null, "description": "İç satış faturası" }
}
```

`salesInvoiceId` response içindeki opsiyonel bağlantıdır. Ödeme bakiyesi her zaman aynı müşteri alacağı/PaymentAllocation verisinden gelir.

## SalesInvoice endpointleri

| İşlem | Endpoint |
| --- | --- |
| Doğrudan fatura + tek satış siparişi | `POST /api/accounting/sales-invoices` |
| Mevcut satıştan fatura üret | `POST /api/accounting/sales-invoices/from-order/{accountingSalesOrderId}` |
| Faturayı genel olarak güncelle (başlık + tüm satırlar) | `PUT /api/accounting/sales-invoices/{id}` |
| Tekil satır işlemleri (geriye dönük uyumluluk) | `POST`, `PUT`, `DELETE /api/accounting/sales-invoices/{id}/lines/...` |
| Post | `POST /api/accounting/sales-invoices/{id}/post` |
| İptal | `POST /api/accounting/sales-invoices/{id}/cancel` |
| Detay/liste | `GET /api/accounting/sales-invoices/{id}`, `GET /api/accounting/sales-invoices` |

Mevcut satıştan fatura üretme body:

```json
{
  "header": {
    "invoiceNumber": "SF-2026-002",
    "invoiceDate": "2026-07-28T00:00:00Z",
    "dueDate": null,
    "description": "Sonradan kesilen fatura"
  }
}
```

Doğrudan fatura endpointi body'si:

```json
{
  "orderHeader": { "currentAccountId": "customer-guid", "orderNumber": "SAT-2026-003", "orderDate": "2026-07-27T00:00:00Z" },
  "invoiceHeader": { "invoiceNumber": "SF-2026-003", "invoiceDate": "2026-07-27T00:00:00Z", "dueDate": null, "description": null },
  "lines": [{ "lineNumber": 1, "productVariantId": "variant-guid", "quantity": 1, "unitOfMeasure": "Adet", "unitsPerSaleUnit": 1, "priceEntryMode": 1, "vatRate": 20, "enteredUnitPrice": 250 }]
}
```

Bu istekte de `Idempotency-Key` gereklidir. Aynı anahtarla retry, ikinci satış/fatura üretmez.

## Faturayı fatura ID'siyle genel olarak güncelleme

Taslak bir faturanın güncel halini tek istekte göndermek için aşağıdaki endpoint kullanılır:

```http
PUT /api/accounting/sales-invoices/{invoiceId}
Content-Type: application/json
```

```json
{
  "header": {
    "invoiceNumber": "SF-2026-003-R1",
    "invoiceDate": "2026-07-29T00:00:00Z",
    "dueDate": "2026-08-12T00:00:00Z",
    "description": "Güncellenmiş fatura"
  },
  "lines": [
    {
      "lineNumber": 1,
      "productVariantId": "variant-guid-1",
      "quantity": 3,
      "unitOfMeasure": "Adet",
      "unitsPerSaleUnit": 1,
      "priceEntryMode": 1,
      "vatRate": 20,
      "enteredUnitPrice": 250,
      "isInvoiceDiscountEligible": true
    },
    {
      "lineNumber": 2,
      "productVariantId": "variant-guid-2",
      "quantity": 1,
      "unitOfMeasure": "Adet",
      "unitsPerSaleUnit": 1,
      "priceEntryMode": 1,
      "vatRate": 10,
      "enteredUnitPrice": 100,
      "isInvoiceDiscountEligible": true
    }
  ]
}
```

`lines` faturanın tamamıdır. Önceki faturada bulunan ancak bu listede bulunmayan satırlar kaldırılır; listedeki satırlar yeni fatura hali olarak kaydedilir. Satır numaraları tekil olmalıdır ve en az bir satır gönderilmelidir. Aynı satır numarasındaki mevcut ürün varyantı değiştirilemez; varyant değişikliği için eski satırı listeden çıkarıp yeni satırı farklı bir `lineNumber` ile göndermek gerekir. Katalog snapshot'ı sunucu tarafından yeniden doğrulanır.

Bu işlem yalnızca `Draft` faturada çalışır. Fatura, bağlı `AccountingSalesOrder` ile aynı transaction içinde güncellenir; toplamlar ve fatura satırı snapshot'ları yeniden hesaplanır. Taslak güncelleme stok, FIFO, cari alacak veya finansal hareket oluşturmaz. `Posted` veya `Cancelled` faturalar düzenlenemez.

`lines` alanı gönderilmezse eski istemciler için yalnızca başlık güncellemesi geriye dönük olarak desteklenir. Yeni frontend akışında her zaman güncel satır listesinin gönderilmesi önerilir.

Başarılı response, `GET /api/accounting/sales-invoices/{invoiceId}` ile aynı `SalesInvoiceDto` yapısındadır. Hatalar `ProblemDetails` formatında döner; geçersiz satır/numara için `400`, bulunamayan fatura için `404`, posted/cancelled veya kimlik değişikliği için `409` kullanılabilir.

## Detay response'unda önemli alanlar

```json
{
  "id": "order-guid",
  "orderNumber": "SAT-2026-001",
  "currentAccountId": "customer-guid",
  "status": 2,
  "grandTotalIncludingVat": 650,
  "paidAmount": 200,
  "remainingAmount": 450,
  "totalCostOfGoodsSold": 320,
  "grossProfitExcludingVat": 180,
  "grossProfitMargin": 36,
  "salesInvoiceId": null,
  "items": []
}
```

`status`: `1 Draft`, `2 Posted`, `3 Cancelled`. UI yalnız taslak kayıtta kalem ve başlık düzenleme aksiyonlarını göstermelidir. `paidAmount` ve `remainingAmount` kullanıcı tarafından güncellenmez; geçerli PaymentAllocation kayıtlarından türetilir.

## İptal davranışı

- AccountingSalesOrder iptali ters StockMovement, FIFO reversal ve müşteri alacağı reversal oluşturur.
- Bağlı SalesInvoice varsa order iptali fatura durumunu da senkronize eder.
- SalesInvoice tek başına iptal edilirse fiziksel stok hareketi oluşturmaz veya silmez.
- Tüm iptal endpointlerinde `{ "reason": "..." }` body kullanılır.

## İndirim alanları

Satır ve başlıkta isteğe bağlı indirim alanları kullanılabilir. `DiscountType`, `DiscountTaxBasis` ve `DiscountUnitBasis` sayısal enum değerleri için `01-genel-kurallar.md` dosyasına bakın. Sabit birim indirimi kullanılıyorsa uygun birim bazını göndermek gerekir; fatura düzeyi sabit indirim yalnız header'da kullanılmalıdır.
