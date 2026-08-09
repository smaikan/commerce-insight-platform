# Muhasebe İş Kuralları ve Kapsam Sözleşmesi

Bu dosya, Accounting API'de bulunan ancak diğer endpoint dokümanlarında dağınık kalan kuralları ve henüz uygulanmamış kapsamları tek yerde toplar. “Mevcut” ifadeleri bugün kullanılabilir davranışı, “kapsam dışı” ifadeleri ise frontend'in endpoint beklememesi gereken alanları gösterir.

## 1. Cari hesap ve kaynak hareketleri

Cari hesap tek master kaynaktır:

```http
POST /api/accounting/current-accounts
PUT  /api/accounting/current-accounts/{id}
GET  /api/accounting/current-accounts
GET  /api/accounting/current-accounts/{id}
```

`CurrentAccountTransaction` ayrı ve değişmez bir muhasebe hareketidir. Mevcut kaynak türleri satış siparişi alacağı, alış faturası borcu, ödeme ve finansal harekettir. Ödeme her zaman `CurrentAccountTransactionId` üzerinden tahsis edilir; doğrudan SalesInvoice'a tahsis yapılmaz.

Cari hareket tipleri:

- `CustomerReceivable` / `CustomerCollection`
- `SupplierDebt` / `SupplierPayment`
- Bunların reversal tipleri

Cari açılış borcu/alacağı ve manuel borç/alacak dekontu henüz yoktur.

## 2. Faturasız tediye ve avans

Tedarikçi faturasına bağlı olmadan tediye oluşturulabilir:

```http
POST /api/accounting/payments
Idempotency-Key: TED-ADV-2026-001
```

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
  "referenceNumber": "TED-ADV-001",
  "description": "Faturasız tedarikçi avansı",
  "allocations": []
}
```

Bu yalnızca `SupplierPayment` (`type: 2`) için geçerlidir. `CustomerCollection` için allocation zorunludur. Boş allocation'lı tediye:

- cari hesapta `SupplierPayment` hareketi oluşturur,
- seçilen kasa/bankada `FinancialTransaction` çıkışı oluşturur,
- `allocatedAmount = 0` ve `unallocatedAmount = amount` döner,
- otomatik olarak daha sonra oluşturulacak faturaya bağlanmaz.

Bir tediye mevcut borçlara tahsis ediliyorsa allocation toplamı ödeme tutarına eşit olmalıdır. Bir ödeme yalnızca tek `CurrentAccountId` içindeki borçlara dağıtılabilir.

## 3. Açılış bakiyeleri

Mevcut olan açılış özelliği stok maliyetidir:

```http
GET   /api/accounting/inventory-cost-layers/opening-balance/by-variant/{productVariantId}
PATCH /api/accounting/inventory-cost-layers/{id}/opening-balance-cost
```

Tüketilmiş cost layer geçmişi değiştirilmez; yalnız kalan miktarın gelecekteki maliyeti güncellenebilir.

Cari açılış borcu/alacağı, kasa açılış bakiyesi, banka açılış bakiyesi, açılış tarihi ve karşı hesap kaydı uygulanmamıştır.

## 4. İade ve dekont kapsamı

E-ticaret tarafında müşteri iade talebi, iade kabulü ve stok geri alma akışları bulunabilir; bunlar Accounting satış/alış iade belgesi değildir.

Accounting tarafında şu an aşağıdakiler yoktur:

- Satış iade faturası
- Alış iade faturası
- Kısmi iade için cari/ödeme otomasyonu
- İadenin Payment ile otomatik eşleştirilmesi
- Cari borç/alacak dekontu
- Fiyat farkı veya kur farkı dekontu
- Dekont iptali ve ters kaydı

`FinancialTransactionType.Refund` yalnızca manuel finans hareketi türüdür; tek başına satış iade süreci değildir.

## 5. Ödeme ve tahsis kuralları

```http
GET  /api/accounting/payments
GET  /api/accounting/payments/{id}
POST /api/accounting/payments/{id}/cancel
```

Kurallar:

- Tam olarak bir kasa veya banka hesabı seçilir.
- Ödeme tutarı pozitiftir.
- TRY ve kur `1` sözleşmesi geçerlidir.
- Geçerli allocation toplamı ödeme tutarını aşamaz.
- Allocation, cari hareketin kalan tutarını aşamaz.
- Reversed/cancelled hareketlere yeni tahsis yapılamaz.
- Aynı ödeme aynı cari hareketi iki kez içeremez.
- İptal, allocation'ları ve finansal etkileri silmeden reversal olarak işaretler.

Tahsise açık hareketler için ayrı seçim endpointi yoktur. Frontend mevcut `receivables`, `debts` ve `overdue-*` raporlarını okuyabilir; ancak bu raporlar ödeme oluşturma öncesi kilitli allocation sorgusu değildir.

## 6. Kasa ve banka

```http
POST /api/accounting/cash-accounts
GET  /api/accounting/cash-accounts
GET  /api/accounting/cash-accounts/{id}/statement
POST /api/accounting/bank-accounts
GET  /api/accounting/bank-accounts
GET  /api/accounting/bank-accounts/{id}/statement
POST /api/accounting/financial-transactions
POST /api/accounting/financial-transactions/bank-transfers
POST /api/accounting/financial-transactions/{id}/reverse
```

Bakiye doğrudan güncellenmez; `FinancialTransaction` kayıtlarından hesaplanır. Mevcut finans türleri CashIn, CashOut, banka transferleri, POS/pazaryeri komisyonu, banka komisyonu, Refund ve ödeme kaynaklı giriş/çıkışlardır.

Devir, banka mutabakatı, banka ekstresi içe aktarma, çek ve senet yoktur.

## 7. Gider yaşam döngüsü

Mevcut endpointler:

```http
POST /api/accounting/expenses/categories
GET  /api/accounting/expenses/categories
POST /api/accounting/expenses
GET  /api/accounting/expenses
POST /api/accounting/purchase-invoices/{id}/expenses
GET  /api/accounting/purchase-invoices/{id}/expenses
```

PurchaseInvoice gideri KDV hariç tutar, miktar veya manuel yöntemle satırlara dağıtılır ve fatura final maliyetini yeniden hesaplar. Genel gider stok maliyetini değiştirmez.

Genel gider için Draft/Post/Cancelled durumu, güncelleme, silme, iptal, cari borç, ödeme, kasa/banka ve dosya numarası alanları yoktur.

## 8. Belge numarası, döviz ve vergi

- Fatura ve sipariş numaralarında mevcut iş kurallarına göre benzersizlik kontrolleri vardır.
- Merkezi otomatik numara üretimi yoktur.
- Payment reference number isteğe bağlıdır; ortak ödeme/dekont sıra servisi yoktur.
- CurrencyCode üç harfli normalize edilir, Accounting API pratikte TRY kullanır.
- ExchangeRate şu an `1` olmalıdır.
- KDV oranı hesaplama ve alış/satış KDV oran raporu vardır.
- KDV istisnası, tevkifat, stopaj, ÖTV, kur kaydı ve vergi dönemi kapatma yoktur.

## 9. Rapor sözleşmesi

Tüm raporlar salt okunur ve `PagedResult<AccountingReportRowDto>` döner. Ortak filtreler:

```text
pageNumber=1&pageSize=20&from=...&to=...&search=...&hasSalesInvoice=true
```

Varsayılan sıralama tarih azalan, sonra ID artandır. Serbest `sortBy` yoktur. Boş sonuçta `items: []` ve `totalCount: 0` döner.

Genel alan anlamları rapor türüne göre şöyledir:

| Rapor grubu | `amount` | `secondaryAmount` | `tertiaryAmount` |
| --- | --- | --- | --- |
| Satış | Genel toplam | FIFO toplam maliyet | KDV hariç brüt kâr |
| Kârlılık | Satış tutarı | FIFO maliyeti | Brüt kâr |
| Cari hareket | Borç | Alacak | Tahsis/reversal sonrası kalan |
| Ödeme | Ödeme tutarı | Tahsis edilen | Tahsis edilmemiş |
| Finans hareketi | İşaretli net hareket | Mutlak hareket | Kullanılmaz |
| Cost layer | Kalan değer veya toplam değer | Birim maliyet | Toplam maliyet |
| KDV | KDV hariç toplam | KDV | KDV dahil toplam |

Raporlarda toplam satırı, devreden bakiye, Excel/PDF dışa aktarma ve yazdırma formatı yoktur.

## 10. Taslak, iptal ve concurrency

AccountingSalesOrder, SalesInvoice ve PurchaseInvoice için Draft/Post/Cancelled yaşam döngüsü vardır. Posted veya Cancelled belge doğrudan düzenlenemez; iptal ve reversal geçmişi korunur. Payment iptali ve FinancialTransaction reversal idempotenttir. Posting ve ödeme işlemleri transaction/concurrency kontrolüyle yürür.

Genel gider, dönem kapatma ve tüm belge türlerini kapsayan ortak kilitleme matrisi uygulanmamıştır.

## 11. Yetki, dosya ve seçim listeleri

- Tüm Accounting controller'ları `AdminOnly` policy kullanır.
- Admin dışı rol matrisi, kasa/banka bazlı yetki ve onay akışı yoktur.
- Fatura, fiş veya ödeme dosyası ekleme/indirme/silme/arşivleme yoktur.
- Cari hesap, ürün/varyant, kasa/banka ve gider kategorisi seçim listeleri mevcut genel liste endpointlerinden alınabilir.
- Tahsise açık cari hareketler için özel seçim endpointi yoktur.

## 12. Hata ve dönem sözleşmesi

API ProblemDetails kullanır. Frontend en az `400`, `401`, `403`, `404`, `409` ve `429` durumlarını ele almalıdır. Validation alanları `errors` altında, genel hata kodu ProblemDetails uzantısında döner. Accounting'e özel bütün hata kodlarının kataloglanmış sabit listesi henüz yoktur.

Mali dönem tanımı, dönem kilitleme, kapanmış dönemde belge oluşturma/iptal kontrolü ve dönem sonu raporları uygulanmamıştır.

## 13. Entegrasyon kapsamı

Harici e-fatura/e-arşiv sağlayıcısı, e-fatura gönderimi ve e-fatura iptali kapsam dışıdır. Banka entegrasyonu, banka ekstresi aktarımı, ERP/dış muhasebe aktarımı ve otomatik pazaryeri komisyon aktarımı da uygulanmamıştır.
