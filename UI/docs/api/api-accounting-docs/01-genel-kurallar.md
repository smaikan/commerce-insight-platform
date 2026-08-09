# Genel Kurallar

## Temel adres ve yetkilendirme

Endpointler `/api/accounting` kökünden başlar. Her istekte yönetici erişim token'ı gönderilmelidir:

```http
Authorization: Bearer <access-token>
Content-Type: application/json
```

Yetkisiz istek `401`, admin olmayan kullanıcı `403` döner.

## JSON ve enumlar

API, enumlar için `JsonStringEnumConverter` kullanmıyor. Bu nedenle request body'lerinde enum değerleri **sayı** olarak gönderilmelidir. Örneğin `PaymentType.CustomerCollection` değeri `1`'dir.

Tarih alanları ISO-8601 formatında gönderilmelidir:

```json
"invoiceDate": "2026-07-27T10:30:00Z"
```

Para alanlarında JSON number kullanılır. UI'da gösterim iki ondalık hane olabilir; hesaplama sonucu API tarafından belirlenir.

## Sayfalama

Sayfalı liste ve rapor endpointleri ortak response biçimini kullanır:

```json
{
  "items": [],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 0,
  "totalPages": 0,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

Yaygın query parametreleri: `pageNumber=1&pageSize=20`.

## Idempotency

Tekrarlanınca ikinci stok veya finans etkisi yaratmaması gereken isteklerde aşağıdaki header zorunludur:

```http
Idempotency-Key: 2af3a2d5-0d9b-4b4f-a034-6e1c4b836204
```

Bu header şu endpointlerde kullanılır:

- `POST /sales-orders`
- `POST /sales-invoices`
- `POST /payments`
- `POST /financial-transactions`
- `POST /financial-transactions/bank-transfers`

İstemci aynı kullanıcı işlemi için aynı anahtarı tekrar göndermeli; yeni işlem için yeni UUID üretmelidir.

## Hatalar

Hatalar ProblemDetails tabanlıdır. UI, `status`, `title`, `detail` ve `code` alanını göstermelidir. `traceId` destek taleplerinde kullanılmalıdır.

```json
{
  "type": "https://httpstatuses.com/409",
  "title": "Conflict",
  "status": 409,
  "detail": "The payment allocation exceeds the remaining receivable.",
  "code": "conflict",
  "traceId": "00-..."
}
```

| Kod | Anlamı | UI davranışı |
| --- | --- | --- |
| 400 | Validation veya iş kuralı | Alan/genel hata göster |
| 401 | Token yok/geçersiz | Oturumu yenile veya girişe yönlendir |
| 403 | Admin yetkisi yok | Yetki mesajı göster |
| 404 | Kayıt bulunamadı | Listeye dön veya yenile |
| 409 | Çakışma, duplicate, concurrency | Veriyi yenile; kullanıcıya tekrar deneme mesajı göster |
| 429 | İstek limiti | Kısa süre sonra yeniden dene |

## Ortak enum referansı

| Alan | Değerler |
| --- | --- |
| `CurrentAccountType` | `1 Customer`, `2 Supplier`, `3 CustomerAndSupplier` |
| `InvoiceStatus` | `1 Draft`, `2 Posted`, `3 Cancelled` |
| `PriceEntryMode` | `1 ExcludingVat`, `2 IncludingVat` |
| `DiscountType` | `1 Percentage`, `2 FixedPerUnit`, `3 FixedLineTotal`, `4 FixedInvoiceTotal` |
| `DiscountTaxBasis` | `1 ExcludingVat`, `2 IncludingVat` |
| `DiscountUnitBasis` | `1 PurchaseUnit`, `2 SaleUnit`, `3 StockUnit` |
| `ShippingPayer` | `0 None`, `1 Seller`, `2 Customer` |

## İptal isteği

İptal ve reversal endpointleri aynı body'yi kullanır:

```json
{ "reason": "Müşteri talebiyle iptal edildi." }
```

Başarılı response:

```json
{ "id": "guid", "status": "Cancelled", "alreadyProcessed": false }
```

İstek ikinci kez güvenle gönderilirse `alreadyProcessed: true` dönebilir.
