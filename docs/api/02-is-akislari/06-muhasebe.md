# Muhasebe

Muhasebe endpointlerinin tamamı **Admin** yetkisi gerektirir. Muhasebe belgeleri e-ticaret `Order` modelinden ayrıdır ve çoğunlukla UUID kullanır.

## Temel kural

Draft belge muhasebe, stok veya cari bakiyeyi kesinleştirmez. `post`, `cancel`, `reverse` gibi yaşam döngüsü operasyonları ayrı ve denetlenebilir işlemlerdir.

## Modüller

| Modül | Endpoint kökü | Amaç |
| --- | --- | --- |
| Cari hesaplar | `/api/accounting/current-accounts` | Müşteri/tedarikçi cari kartları ve bakiye bağlamı |
| Satış siparişleri | `/api/accounting/sales-orders` | Muhasebe satış siparişi; e-ticaret Order değildir |
| Satış faturaları | `/api/accounting/sales-invoices` | Fatura, satır, post ve cancel işlemleri |
| Alış faturaları | `/api/accounting/purchase-invoices` | Tedarikçi faturası, satır, gider ve stok dağıtımı |
| Giderler | `/api/accounting/expenses` | Gider kategorisi ve gider kaydı |
| Ödemeler | `/api/accounting/payments` | Tahsilat/ödeme ve iptal |
| Kasa hesapları | `/api/accounting/cash-accounts` | Kasa hesabı ve ekstre |
| Banka hesapları | `/api/accounting/bank-accounts` | Banka hesabı ve ekstre |
| Finansal hareketler | `/api/accounting/financial-transactions` | Manuel hareket, transfer ve reverse |
| Maliyet | `/api/accounting/inventory-cost-layers` | Açılış maliyeti ve FIFO katmanları |
| Raporlar | `/api/accounting/reports` | 28 ayrı operasyonel ve mali rapor |

## Örnek cari hesap oluşturma

```http
POST /api/accounting/current-accounts
Authorization: Bearer <admin-access-token>
Content-Type: application/json
```

```json
{
  "name": "Örnek Tedarikçi A.Ş.",
  "type": 1,
  "taxNumber": "1234567890",
  "taxOffice": "Kadıköy",
  "email": "muhasebe@example.com",
  "phone": "+902121112233",
  "currencyCode": "TRY"
}
```

Alanlar ve enum değerleri için endpoint sayfası esas alınmalıdır.

## Belge yaşam döngüsü

Genel yaklaşım:

1. Draft belge oluşturulur.
2. Satırlar eklenir/güncellenir.
3. Gerekli stok/gider dağıtımları yapılır.
4. `post` ile kesinleştirilir.
5. Hatalı kesin kayıt doğrudan silinmez; sözleşmesine göre `cancel` veya `reverse` uygulanır.

Bu işlemler idempotency ve durum kurallarına tabidir. Posted belgeyi normal update endpointiyle değiştirilebilir varsaymayın.

## Raporlar

Rapor endpointleri sayfalıdır ve `From`, `To`, `Search`, `Id`, `HasSalesInvoice` gibi ilgili filtreleri kabul eder. Filtrelerin tamamı her raporda anlamlı olmayabilir; ilgili endpoint sayfasına bakın.

Örnek:

```http
GET /api/accounting/reports/profitability/products?PageNumber=1&PageSize=50&From=2026-08-01T00:00:00Z&To=2026-08-31T23:59:59Z
Authorization: Bearer <admin-access-token>
```

## Ayrıntılı referans

- [Muhasebe raporları](../03-endpoint-referansi/05-muhasebe/raporlar/README.md)
- [Satış ve fatura](../03-endpoint-referansi/05-muhasebe/satis-siparisleri/README.md)
- [Alış ve gider](../03-endpoint-referansi/05-muhasebe/alis-faturalari/README.md)
- [Tahsilat, kasa ve banka](../03-endpoint-referansi/05-muhasebe/finansal-hareketler/README.md)
- [Cari ve maliyet](../03-endpoint-referansi/05-muhasebe/cari-hesaplar/README.md)

