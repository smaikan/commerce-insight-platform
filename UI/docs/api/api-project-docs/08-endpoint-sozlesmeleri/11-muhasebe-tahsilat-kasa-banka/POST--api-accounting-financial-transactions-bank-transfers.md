# POST /api/accounting/financial-transactions/bank-transfers

- İşlev alanı: **11 Muhasebe tahsilat, kasa ve banka**
- İşlev: Yeni kaynak veya iş akışı adımı oluşturur/başlatır.
- Operation ID: `POST-/api/accounting/financial-transactions/bank-transfers`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `Idempotency-Key` | header | Hayır | string (uuid) |

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `fromBankAccountId` | string (uuid) | Evet |
| `toBankAccountId` | string (uuid) | Evet |
| `amount` | number (double) | Evet |
| `transactionDate` | string (date-time) | Evet |
| `currencyCode` | string | Evet |
| `description` | string | Hayır |

```json
{
    "fromBankAccountId":  "00000000-0000-0000-0000-000000000001",
    "toBankAccountId":  "00000000-0000-0000-0000-000000000001",
    "amount":  1,
    "transactionDate":  "2026-07-29T12:00:00Z",
    "currencyCode":  "string",
    "description":  "string"
}
```

## Başarılı response (200)

```json
{
    "transferOut":  {
                        "id":  "00000000-0000-0000-0000-000000000001",
                        "cashAccountId":  "00000000-0000-0000-0000-000000000001",
                        "bankAccountId":  "00000000-0000-0000-0000-000000000001",
                        "type":  1,
                        "direction":  1,
                        "amount":  1,
                        "balanceAfter":  1,
                        "currencyCode":  "string",
                        "transactionDate":  "2026-07-29T12:00:00Z",
                        "sourceType":  1,
                        "sourceId":  "00000000-0000-0000-0000-000000000001",
                        "description":  "string",
                        "reversesTransactionId":  "00000000-0000-0000-0000-000000000001",
                        "createdBy":  1,
                        "createdAt":  "2026-07-29T12:00:00Z"
                    },
    "transferIn":  {
                       "id":  "00000000-0000-0000-0000-000000000001",
                       "cashAccountId":  "00000000-0000-0000-0000-000000000001",
                       "bankAccountId":  "00000000-0000-0000-0000-000000000001",
                       "type":  1,
                       "direction":  1,
                       "amount":  1,
                       "balanceAfter":  1,
                       "currencyCode":  "string",
                       "transactionDate":  "2026-07-29T12:00:00Z",
                       "sourceType":  1,
                       "sourceId":  "00000000-0000-0000-0000-000000000001",
                       "description":  "string",
                       "reversesTransactionId":  "00000000-0000-0000-0000-000000000001",
                       "createdBy":  1,
                       "createdAt":  "2026-07-29T12:00:00Z"
                   }
}
```

