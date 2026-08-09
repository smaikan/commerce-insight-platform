# Ödemeler, Tahsilatlar, Kasa ve Banka

PaymentAllocation, doğrudan `CurrentAccountTransaction` hedefler. Frontend satış faturası var/yok ayrımı yapmamalıdır; müşteri alacağı veya tedarikçi borcu cari hareketini seçmelidir.

## Payment endpointleri

| İşlem | Endpoint |
| --- | --- |
| Tahsilat/ödeme oluştur | `POST /api/accounting/payments` |
| Detay | `GET /api/accounting/payments/{id}` |
| Liste | `GET /api/accounting/payments?pageNumber=1&pageSize=20` |
| İptal | `POST /api/accounting/payments/{id}/cancel` |

`Idempotency-Key` header zorunludur.

## Müşteri tahsilatı örneği

```json
{
  "currentAccountId": "customer-guid",
  "type": 1,
  "amount": 600,
  "paymentDate": "2026-07-27T00:00:00Z",
  "cashAccountId": "cash-guid",
  "bankAccountId": null,
  "currencyCode": "TRY",
  "exchangeRate": 1,
  "referenceNumber": "MAK-001",
  "description": "Kısmi tahsilat",
  "allocations": [
    { "currentAccountTransactionId": "receivable-guid", "amount": 600 }
  ]
}
```

## Tedarikçi ödemesi örneği

```json
{
  "currentAccountId": "supplier-guid",
  "type": 2,
  "amount": 1250,
  "paymentDate": "2026-07-27T00:00:00Z",
  "cashAccountId": null,
  "bankAccountId": "bank-guid",
  "allocations": [
    { "currentAccountTransactionId": "supplier-debt-guid", "amount": 1000 },
    { "currentAccountTransactionId": "supplier-debt-guid-2", "amount": 250 }
  ]
}
```

`PaymentType`: `1 CustomerCollection`, `2 SupplierPayment`. Tam olarak bir finans hesabı gönderilmelidir: `cashAccountId` veya `bankAccountId`.

## Faturasız tediye / tedarikçi avansı

Tedarikçiye fatura veya mevcut `SupplierDebt` hareketi olmadan ödeme yapılabilir. Bu durumda `SupplierPayment` için `allocations` boş gönderilir. Kayıt, cari hesapta `SupplierPayment` hareketi ve seçilen kasa/bankada finansal çıkış oluşturur; ödeme `unallocatedAmount` kadar tedarikçi avansı olarak kalır.

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

Bu özel durum yalnız `SupplierPayment` için geçerlidir. `CustomerCollection` için en az bir müşteri alacağı tahsisi zorunludur. Faturasız tediye sonradan otomatik olarak bir faturaya bağlanmaz; tahsis yapılabilen mevcut bir borç hareketi varsa ödeme oluşturulurken tahsis edilmelidir. Ödemenin `allocatedAmount` değeri `0`, `unallocatedAmount` değeri ödeme tutarı olur.

Kurallar:

- Tahsislerin toplamı ödeme tutarını geçemez.
- Aynı ödeme birden fazla cari harekete dağıtılabilir.
- Cari hareketin kalan borç/alacağı geçilemez.
- İptal/ters kayıtlı harekete tahsis yapılamaz.
- Payment iptal edilirse tahsisler ve finans hareketi terslenir; UI response'taki `status` alanını yenilemelidir.

## Kasa ve banka hesapları

| İşlem | Endpoint |
| --- | --- |
| Kasa oluştur/liste/ekstre | `POST /cash-accounts`, `GET /cash-accounts`, `GET /cash-accounts/{id}/statement` |
| Banka oluştur/liste/ekstre | `POST /bank-accounts`, `GET /bank-accounts`, `GET /bank-accounts/{id}/statement` |

Kasa body:

```json
{ "code": "KASA-TRY", "name": "Merkez Kasa", "currencyCode": "TRY" }
```

Banka body:

```json
{ "code": "BANK-TRY", "name": "Ana Hesap", "bankName": "Örnek Banka", "iban": "TR000000000000000000000000", "currencyCode": "TRY" }
```

`balance` alanı doğrudan güncellenmez; FinancialTransaction kayıtlarından türetilir. Ekstre response'unda her hareket için `balanceAfter` vardır.

## Manuel finans hareketi

```http
POST /api/accounting/financial-transactions
Idempotency-Key: 22222222-2222-2222-2222-222222222222
```

```json
{
  "type": 10,
  "amount": 500,
  "transactionDate": "2026-07-27T00:00:00Z",
  "cashAccountId": "cash-guid",
  "bankAccountId": null,
  "currencyCode": "TRY",
  "description": "Kasaya manuel giriş"
}
```

`FinancialTransactionType`: `10 CashIn`, `11 CashOut`, `30 PosCollection`, `40 BankCommission`, `41 MarketplaceCommission`, `50 Refund`. Ödeme kaynaklı `CustomerCollection` ve `SupplierPayment` türleri Payment endpointi tarafından oluşturulur.

Bir finans hareketini terslemek için:

```http
POST /api/accounting/financial-transactions/{id}/reverse
```

```json
{ "reason": "Yanlış kasa hesabı seçildi." }
```

## Banka transferi

```http
POST /api/accounting/financial-transactions/bank-transfers
Idempotency-Key: 33333333-3333-3333-3333-333333333333
```

```json
{
  "fromBankAccountId": "source-bank-guid",
  "toBankAccountId": "target-bank-guid",
  "amount": 1000,
  "transactionDate": "2026-07-27T00:00:00Z",
  "currencyCode": "TRY",
  "description": "Şubeler arası transfer"
}
```

Response, atomik olarak oluşturulan `transferOut` ve `transferIn` kayıtlarını içerir.
