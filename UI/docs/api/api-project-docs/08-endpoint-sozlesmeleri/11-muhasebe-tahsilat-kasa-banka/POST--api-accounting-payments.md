# POST /api/accounting/payments

- İşlev alanı: **11 Muhasebe tahsilat, kasa ve banka**
- İşlev: Yeni kaynak veya iş akışı adımı oluşturur/başlatır.
- Operation ID: `POST-/api/accounting/payments`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `Idempotency-Key` | header | Hayır | string |

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `currentAccountId` | string (uuid) | Evet |
| `type` | integer (int32) | Evet |
| `amount` | number (double) | Evet |
| `paymentDate` | string (date-time) | Evet |
| `allocations` | array | Evet |
| `cashAccountId` | string (uuid) | Hayır |
| `bankAccountId` | string (uuid) | Hayır |
| `currencyCode` | string | Evet |
| `exchangeRate` | number (double) | Evet |
| `referenceNumber` | string | Hayır |
| `description` | string | Hayır |

```json
{
    "currentAccountId":  "00000000-0000-0000-0000-000000000001",
    "type":  1,
    "amount":  1,
    "paymentDate":  "2026-07-29T12:00:00Z",
    "allocations":  {
                        "currentAccountTransactionId":  "00000000-0000-0000-0000-000000000001",
                        "amount":  1
                    },
    "cashAccountId":  "00000000-0000-0000-0000-000000000001",
    "bankAccountId":  "00000000-0000-0000-0000-000000000001",
    "currencyCode":  "string",
    "exchangeRate":  1,
    "referenceNumber":  "string",
    "description":  "string"
}
```

## Başarılı response (200)

```json
{
    "id":  "00000000-0000-0000-0000-000000000001",
    "currentAccountId":  "00000000-0000-0000-0000-000000000001",
    "type":  1,
    "direction":  1,
    "status":  1,
    "amount":  1,
    "allocatedAmount":  1,
    "unallocatedAmount":  1,
    "currencyCode":  "string",
    "paymentDate":  "2026-07-29T12:00:00Z",
    "cashAccountId":  "00000000-0000-0000-0000-000000000001",
    "bankAccountId":  "00000000-0000-0000-0000-000000000001",
    "referenceNumber":  "string",
    "description":  "string",
    "createdAt":  "2026-07-29T12:00:00Z",
    "cancelledBy":  1,
    "cancelledAt":  "2026-07-29T12:00:00Z",
    "cancellationReason":  "string",
    "allocations":  {
                        "id":  "00000000-0000-0000-0000-000000000001",
                        "currentAccountTransactionId":  "00000000-0000-0000-0000-000000000001",
                        "sourceType":  1,
                        "sourceId":  "00000000-0000-0000-0000-000000000001",
                        "allocatedAmount":  1,
                        "isReversed":  true,
                        "reversedAt":  "2026-07-29T12:00:00Z"
                    }
}
```

