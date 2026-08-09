# Frontend Sözleşmeleri ve Merkezi Referans

Bu dosya, Accounting API'de kod karşılığı bulunan frontend sözleşmelerini toplar. JSON'da enumlar sayısal gönderilir. `Guid` alanları UUID metni, `DateTime` alanları ISO-8601 metni, `decimal` alanları JSON number olarak gönderilir.

## 1. Tam request/response modelleri

### Cari hesap

`CurrentAccountInput` request:

```ts
{ code: string; type: 1|2|3; name: string; tradeName?: string|null;
  nationalIdentityNumber?: string|null; taxNumber?: string|null; taxOffice?: string|null;
  phoneNumber?: string|null; email?: string|null; country?: string|null; city?: string|null;
  district?: string|null; neighborhood?: string|null; addressLine?: string|null;
  postalCode?: string|null; userId?: string|null }
```

`CurrentAccountDto`, buna ek olarak `id`, `isActive` ve normalize edilmiş alanları döner. `type`: `1 Customer`, `2 Supplier`, `3 CustomerAndSupplier`.

### Ödeme

```ts
type PaymentAllocationInput = { currentAccountTransactionId: string; amount: number };
type CreatePaymentInput = {
  currentAccountId: string; type: 1|2; amount: number; paymentDate: string;
  allocations: PaymentAllocationInput[]; cashAccountId?: string|null; bankAccountId?: string|null;
  currencyCode?: string; exchangeRate?: number; referenceNumber?: string|null; description?: string|null;
};
type PaymentAllocationDto = {
  id: string; currentAccountTransactionId: string; sourceType: 1|2|3|4|5;
  sourceId: string; allocatedAmount: number; isReversed: boolean; reversedAt?: string|null;
};
type PaymentDto = {
  id: string; currentAccountId: string; type: 1|2; direction: 1|2; status: 1|2|3;
  amount: number; allocatedAmount: number; unallocatedAmount: number; currencyCode: string;
  paymentDate: string; cashAccountId?: string|null; bankAccountId?: string|null;
  referenceNumber?: string|null; description?: string|null; createdAt: string;
  cancelledBy?: number|null; cancelledAt?: string|null; cancellationReason?: string|null;
  allocations: PaymentAllocationDto[];
};
```

`PaymentStatus`: `1 Completed`, `2 Cancelled`, `3 Reversed`. Faturasız tediye için yalnız SupplierPayment boş allocation alabilir; CustomerCollection allocation ister.

### Kasa, banka ve finans hareketi

```ts
type FinancialAccountInput = { code: string; name: string; currencyCode?: string };
type BankAccountInput = { code: string; name: string; bankName: string; iban?: string|null; currencyCode?: string };
type CashAccountDto = { id: string; code: string; name: string; currencyCode: string; isActive: boolean; balance: number };
type BankAccountDto = { id: string; code: string; name: string; bankName: string; iban?: string|null; currencyCode: string; isActive: boolean; balance: number };
type CreateFinancialTransactionInput = { type: number; amount: number; transactionDate: string; cashAccountId?: string|null; bankAccountId?: string|null; currencyCode?: string; description?: string|null };
type FinancialTransactionDto = { id: string; cashAccountId?: string|null; bankAccountId?: string|null; type: number; direction: 1|2; amount: number; balanceAfter: number; currencyCode: string; transactionDate: string; sourceType: number; sourceId: string; description?: string|null; reversesTransactionId?: string|null; createdBy: number; createdAt: string };
type BankTransferInput = { fromBankAccountId: string; toBankAccountId: string; amount: number; transactionDate: string; currencyCode?: string; description?: string|null };
type BankTransferDto = { transferOut: FinancialTransactionDto; transferIn: FinancialTransactionDto };
```

### Gider

```ts
type CreateGeneralExpenseCommand = { categoryId: string; amountExcludingVat: number; vatRate: number; expenseDate: string; description: string };
type ExpenseDto = { id: string; categoryId: string; type: 1|2; amountExcludingVat: number; vatRate: number; vatAmount: number; totalAmountIncludingVat: number; expenseDate: string; description: string };
type PurchaseInvoiceExpenseDto = { id: string; purchaseInvoiceId: string; categoryId: string; allocationMethod: 1|2|3; amountExcludingVat: number; amountIncludingVat: number; allocations: { lineId: string; amountExcludingVat: number; amountIncludingVat: number }[] };
```

### Rapor satırı

Tüm Accounting raporları `PagedResult<AccountingReportRowDto>` döner:

```ts
type AccountingReportRowDto = {
  id: string; relatedId?: string|null; number?: string|null; name?: string|null;
  date?: string|null; dueDate?: string|null; amount: number; secondaryAmount: number;
  tertiaryAmount: number; quantity: number; rate?: number|null;
  hasSalesInvoice?: boolean|null; currencyCode: string;
};
type PagedResult<T> = { items: T[]; pageNumber: number; pageSize: number; totalCount: number; totalPages: number };
```

SalesInvoice, AccountingSalesOrder ve PurchaseInvoice detay DTO'larının tüm alanları [04-satis-siparisleri-ve-faturalar.md](04-satis-siparisleri-ve-faturalar.md) ve [03-alis-ve-giderler.md](03-alis-ve-giderler.md) içindeki response örnekleriyle birlikte kaynak record sırasındadır: snapshot, header, toplamlar, `paidAmount`, `remainingAmount`, maliyet/kâr, lifecycle ve `lines` alanları döner.

Detay response alanlarının eksiksiz listesi:

- `AccountingSalesOrderDto`: `id`, `orderNumber`, `currentAccountId`, `currentAccountName`, `taxNumberSnapshot`, `taxOfficeSnapshot`, `phoneNumberSnapshot`, `emailSnapshot`, `addressSnapshot`, `orderDate`, `dueDate`, `currencyCode`, `exchangeRate`, `status`, `description`, indirim alanları, tüm subtotal/discount/net/shipping/VAT/grand total alanları, `paidAmount`, `remainingAmount`, `totalCostOfGoodsSold`, `grossProfitExcludingVat`, `grossProfitMargin`, `salesInvoiceId`, lifecycle alanları ve `items`.
- `SalesInvoiceDto`: `id`, `accountingSalesOrderId`, `currentAccountId`, cari snapshot alanları, `invoiceNumber`, `invoiceDate`, `dueDate`, `currencyCode`, `exchangeRate`, `status`, `description`, indirim/toplam alanları, `shippingTotal`, `shippingPayer`, `vatTotal`, `grandTotalIncludingVat`, `paidAmount`, `remainingAmount`, maliyet/kâr alanları, lifecycle alanları ve `lines`.
- `PurchaseInvoiceDto`: `id`, `currentAccountId`, cari snapshot alanları, `invoiceNumber`, `invoiceDate`, `dueDate`, `currencyCode`, `exchangeRate`, `status`, `description`, indirim/toplam alanları, `totalFinalCostExcludingVat`, `totalFinalCostIncludingVat`, `paidAmount`, `remainingAmount`, lifecycle alanları ve `lines`.
- Satış/alış line DTO'ları: kimlik ve line number, ürün/varyant snapshot (`productId`, `productVariantId`, `productName`, `variantName`, `sku`, `barcode`), miktar/birim, fiyat/KDV/indirim alanları, net/brüt/KDV toplamları, COGS/final cost, kâr ve ilgili stock/FIFO/allocation listeleri.
- `AccountingSalesOrderItemDto`/`SalesInvoiceLineDto` alanları: `id`, (SalesInvoice için `accountingSalesOrderItemId`), `lineNumber`, `productId`, `productVariantId`, `productName`, `variantName`, `sku`, `barcode`, `quantity`, `unitOfMeasure`, `unitsPerSaleUnit`, `stockQuantity`, `enteredUnitPrice`, `priceEntryMode`, `unitPriceExcludingVat`, `unitPriceIncludingVat`, `vatRate`, `lineDiscountType`, `lineDiscountValue`, `lineDiscountTaxBasis`, `lineDiscountUnitBasis`, `isInvoiceDiscountEligible`, `grossAmountExcludingVat`, `grossAmountIncludingVat`, `lineDiscountAmountExcludingVat`, `lineDiscountAmountIncludingVat`, `invoiceDiscountShareExcludingVat`, `invoiceDiscountShareIncludingVat`, `totalDiscountAmountExcludingVat`, `totalDiscountAmountIncludingVat`, `netAmountExcludingVat`, `vatAmount`, `totalAmountIncludingVat`, `costOfGoodsSold`, `grossProfitExcludingVat`, `grossProfitMargin`; ayrıca order item için `stockMovements` ve `costLayerConsumptions`, invoice line için `costLayerConsumptions`.
- `PurchaseInvoiceLineDto` alanları: `id`, `lineNumber`, `productId`, `productVariantId`, `productName`, `variantName`, `sku`, `barcode`, `purchaseQuantity`, `unitOfMeasure`, `unitsPerPurchaseUnit`, `stockQuantity`, `enteredUnitPrice`, `priceEntryMode`, `unitPriceExcludingVat`, `unitPriceIncludingVat`, `vatRate`, `grossAmountExcludingVat`, `grossAmountIncludingVat`, `totalDiscountAmountExcludingVat`, `totalDiscountAmountIncludingVat`, `netAmountExcludingVat`, `vatAmount`, `totalAmountIncludingVat`, `finalUnitCostExcludingVat`, `finalUnitCostIncludingVat`, `allocations`.

## 2. Alan doğrulama ve uzunluk kuralları

| Alan grubu | Kural |
| --- | --- |
| Cari code/name | Zorunlu; code 50, name 250 karakter |
| Cari trade/tax/identity | TradeName 250, identity/tax number 20, tax office 100 |
| Cari iletişim | Phone 30, email 320; e-posta temel `@` kontrolünden geçer |
| Cari adres | Country/City/District/Neighborhood 150, AddressLine 500, PostalCode 20 |
| Kasa/banka | Code 50, Name 150, BankName 150, IBAN 34 |
| Fatura/sipariş numarası | 100 karakter, boş olamaz; sipariş global, invoice numarası cari hesap kapsamında tekildir |
| Açıklama | Accounting belge/ödeme/finans açıklaması en fazla 500 karakter |
| Payment reference/idempotency | Reference 100, Payment idempotency 100 |
| Gider kategorisi | Code 50, Name 150 |
| Miktar/fiyat | Miktar ve birim katsayısı pozitif; fiyat sıfır veya üzeri |
| KDV | `0..100` aralığı |
| Sayfalama | PageNumber `1..10000`, PageSize `1..100` |

TRY sözleşmesi nedeniyle ödeme, kasa, banka, finans hareketi ve fatura validator'ları `currencyCode = TRY`, `exchangeRate = 1` bekler.

## 3. Merkezi enum referansı

| Enum | Değerler |
| --- | --- |
| InvoiceStatus | 1 Draft, 2 Posted, 3 Cancelled |
| CurrentAccountType | 1 Customer, 2 Supplier, 3 CustomerAndSupplier |
| CurrentAccountTransactionType | 1 SupplierDebt, 2 SupplierDebtReversal, 3 SupplierPayment, 4 SupplierPaymentReversal, 10 CustomerReceivable, 11 CustomerReceivableReversal, 12 CustomerCollection, 13 CustomerCollectionReversal |
| PaymentType | 1 CustomerCollection, 2 SupplierPayment |
| PaymentDirection | 1 In, 2 Out |
| PaymentStatus | 1 Completed, 2 Cancelled, 3 Reversed |
| FinancialTransactionType | 1 CustomerCollection, 2 SupplierPayment, 10 CashIn, 11 CashOut, 20 BankTransferIn, 21 BankTransferOut, 30 PosCollection, 40 BankCommission, 41 MarketplaceCommission, 50 Refund, 60 ReversalIn, 61 ReversalOut |
| FinancialTransactionDirection | 1 In, 2 Out |
| ExpenseType | 1 General, 2 InventoryRelatedPurchase |
| PurchaseExpenseAllocationMethod | 1 VatExclusiveLineAmount, 2 Quantity, 3 Manual |
| PriceEntryMode | 1 ExcludingVat, 2 IncludingVat |
| DiscountType | 1 Percentage, 2 FixedPerUnit, 3 FixedLineTotal, 4 FixedInvoiceTotal |
| DiscountTaxBasis | 1 ExcludingVat, 2 IncludingVat |
| DiscountUnitBasis | 1 PurchaseUnit, 2 SaleUnit, 3 StockUnit |
| ShippingPayer | 0 None, 1 Seller, 2 Customer |
| AccountingSourceType | 1 PurchaseInvoice, 2 SalesInvoice, 3 AccountingSalesOrder, 4 Payment, 5 FinancialTransaction |
| InventoryCostLayerSourceType | 1 PurchaseInvoiceAllocation, 2 OpeningBalance |
| CostLayerStatus | 1 Open, 2 Consumed, 3 Invalidated |

## 4. Endpoint filtre ve sıralama sözleşmesi

| Endpoint grubu | Filtreler | Sıralama |
| --- | --- | --- |
| Current accounts | `pageNumber`, `pageSize` | Repository sırası; `sortBy` yok |
| Payments | `pageNumber`, `pageSize` | PaymentDate/ID deterministik listeleme |
| Sales/Purchase invoices | `pageNumber`, `pageSize` | Tarih ve ID’ye göre deterministik |
| Expenses/categories | `pageNumber`, `pageSize` | Sabit repository sırası |
| Reports | `pageNumber`, `pageSize`, `from`, `to`, `id`, `search`, `hasSalesInvoice` | Date descending, ID ascending |
| Cash/Bank statements | Account ID | Kronolojik hareket ve `balanceAfter` |

`from` değeri `to` değerinden büyük olamaz. Boş listelerde HTTP 200, `items: []`, `totalCount: 0` döner. Serbest sıralama ve export parametreleri uygulanmamıştır.

## 5. Hata kodu kataloğu

ProblemDetails uzantısındaki alanın adı `code`'dur:

| HTTP | code | Frontend davranışı |
| --- | --- | --- |
| 400 | `validation_error` | Alan hatalarını `errors` içinden göster |
| 400 | `business_rule_violation` | `detail` mesajını göster |
| 400 | `bad_request` | İstek formatını düzelt |
| 401 | `authentication_required`, `unauthorized`, `invalid_access_token` | Oturumu yenile/girişe yönlendir |
| 403 | `forbidden` | Yetki mesajı göster |
| 404 | `resource_not_found` | Kaydı yenile veya listeye dön |
| 409 | `conflict` | Güncel detayı tekrar oku; otomatik overwrite yapma |
| 409 | `concurrency_conflict` | Kullanıcıya “kayıt başka kullanıcı tarafından değiştirildi” göster |
| 429 | `rate_limit_exceeded` | Daha sonra tekrar dene |
| 500 | `internal_error` | `traceId` ile destek kaydı oluştur |

Validation response örneği:

```json
{
  "type": "urn:ecommerce:error:validation_error",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": { "Payment.Amount": ["'Amount' must be greater than '0'."] },
  "code": "validation_error",
  "traceId": "00-..."
}
```

Benzersiz fatura/sipariş/kasa/banka/kategori kodu hataları `409 conflict` olarak ele alınmalıdır. Frontend formu korumalı, mevcut listeyi yenilemeli ve kullanıcıdan farklı numara/kod istemelidir; eski kaydı otomatik silmemelidir.

## 6. Seçim listesi response modelleri

- Cari hesap: `PagedResult<CurrentAccountDto>`; yalnız `pageNumber` ve `pageSize` vardır, current-account endpointinde `search` parametresi yoktur.
- Kasa: `IReadOnlyList<CashAccountDto>` (`id`, `code`, `name`, `currencyCode`, `isActive`, `balance`).
- Banka: `IReadOnlyList<BankAccountDto>` (`id`, `code`, `name`, `bankName`, `iban`, `currencyCode`, `isActive`, `balance`).
- Gider kategorisi: `PagedResult<ExpenseCategoryDto>` (`id`, `code`, `name`, `isActive`).
- Ürün/varyant: mevcut Product/ProductVariant API DTO'ları kullanılmalıdır; Accounting ikinci ürün tablosu oluşturmaz.
- Alış fatura stok tahsisi: `IReadOnlyList<AvailableStockMovementDto>` (`id`, `productVariantId`, `quantity`, `allocatedQuantity`, `availableQuantity`, `createdAt`).

Tahsise açık müşteri alacağı/tedarikçi borcu için özel selection endpointi yoktur; mevcut rapor satırları ödeme allocation ekranında geçici seçim olarak kullanılabilir.

## 7. Belge durumu ve UI aksiyon matrisi

| Belge/durum | Görüntüle | Oluştur | Düzenle | Post | İptal/reversal |
| --- | --- | --- | --- | --- | --- |
| AccountingSalesOrder Draft | Evet | - | Header/tam satır | Evet | Evet |
| AccountingSalesOrder Posted | Evet | - | Hayır | Idempotent no-op | Evet; ters StockMovement/FIFO/cari |
| SalesInvoice Draft | Evet | - | Header/tam satır | Evet | Evet |
| SalesInvoice Posted | Evet | - | Hayır | Idempotent no-op | Evet; fiziksel stok oluşturmaz |
| PurchaseInvoice Draft | Evet | - | Header/satır/tahsis/gider | Evet | Evet |
| PurchaseInvoice Posted | Evet | - | Hayır | Idempotent no-op | Evet; tüketilmiş maliyet politikası kontrolü |
| Payment Completed | Evet | Evet | Hayır | - | Cancel/reversal |
| Payment Cancelled/Reversed | Evet | Hayır | Hayır | - | Idempotent no-op |
| FinancialTransaction | Evet | Evet | Hayır | - | Reverse |
| General Expense | Evet | Evet | Şu an yok | Şu an yok | Şu an yok |

İptal sonrası UI, silme yerine detay endpointini yeniden çağırmalıdır. `status`, `cancelledAt`, `cancelledBy`, `cancellationReason`, `IsReversed`/`ReversedAt`, reversal linkleri ve payment `allocatedAmount`/`unallocatedAmount` yeniden okunmalıdır. Orijinal hareket frontend'de silinmez; reversal ayrı hareket olarak gösterilir.

## 8. Cari borç/alacak gösterim standardı

- Customer receivable: `DebitAmount > 0`.
- Supplier debt: `CreditAmount > 0`.
- Customer collection: `CreditAmount > 0`.
- Supplier payment: `DebitAmount > 0`.
- `remaining = original debit/credit - valid allocations - valid reversal amount`.
- Pozitif remaining müşteri için tahsil edilecek alacağı, tedarikçi için ödenecek borcu ifade eder.
- Reversal hareketleri geçmişi koruyan ayrı satırlardır; orijinal satır silinmez.

## 9. Kesin rapor kolon sözleşmesi

| Rapor | Ana kolonlar |
| --- | --- |
| Sales | `number`, `name`, `date`, `amount=GrandTotalIncludingVat`, `secondaryAmount=TotalCostOfGoodsSold`, `tertiaryAmount=GrossProfitExcludingVat`, `quantity`, `rate=GrossProfitMargin`, `hasSalesInvoice` |
| Sales items | `number=OrderNumber`, `name=Product/Variant`, `amount=NetAmountExcludingVat`, `secondaryAmount=CostOfGoodsSold`, `tertiaryAmount=GrossProfitExcludingVat`, `quantity`, `rate=GrossProfitMargin` |
| Sales/Purchase invoices | `number`, `name`, `date`, `dueDate`, `amount` toplam tutar, `hasSalesInvoice` yalnız satışta |
| Customer receivables/Supplier debts | `amount=DebitAmount`, `secondaryAmount=CreditAmount`, `tertiaryAmount=remaining` |
| Payments | `amount=Payment.Amount`, `secondaryAmount=allocated`, `tertiaryAmount=unallocated` |
| Cash/Bank movements | `amount=işaretli net`, `secondaryAmount=mutlak hareket`, `relatedId=hesap` |
| Purchase/Sales VAT | `amount=KDV hariç`, `secondaryAmount=VAT`, `tertiaryAmount=KDV dahil`, `rate=VAT rate` |

Rapor sonuçlarında toplam/devreden bakiye alanları yoktur; frontend sayfa satırlarını genel toplam gibi göstermemelidir.

## 10. Kodda bulunmayan sözleşmeler

Aşağıdakiler için mevcut bir DTO/endpoint sözleşmesi bulunmadığından frontend entegrasyonu kapsam dışıdır:

- Açılış cari/kasa/banka bakiyesi request/response'u
- İade faturası ve kısmi iade DTO'ları
- Borç/alacak dekontu ve fiyat/kur farkı DTO'ları
- Tahsise açık cari hareket selection DTO'su
- Genel gider update/post/cancel/delete DTO'ları
- Dosya ekleme/indirme/silme/arşivleme DTO'ları
- Mali dönem ve dönem kapatma DTO'ları
- Rapor export/yazdırma sözleşmesi
- Banka mutabakatı/ekstre import sözleşmesi
- Çek/senet DTO'ları
- Admin dışı rol ve onay matrisi DTO'ları
