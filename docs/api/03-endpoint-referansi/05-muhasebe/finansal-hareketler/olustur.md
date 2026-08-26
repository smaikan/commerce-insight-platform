# POST /api/accounting/financial-transactions

- Görev alanı: **Muhasebe → Finansal hareketler**.
- İşlev: oluşturur.
- Operation ID: `POST-/api/accounting/financial-transactions`
- Yetki: **Admin**.
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
| `type` | integer (int32) | Evet |
| `amount` | number (double) | Evet |
| `transactionDate` | string (date-time) | Evet |
| `cashAccountId` | string (uuid) | Hayır |
| `bankAccountId` | string (uuid) | Hayır |
| `currencyCode` | string | Evet |
| `description` | string | Hayır |

```json
{
  "type": 1,
  "amount": 1,
  "transactionDate": "2026-07-29T12:00:00Z",
  "cashAccountId": "00000000-0000-0000-0000-000000000001",
  "bankAccountId": "00000000-0000-0000-0000-000000000001",
  "currencyCode": "string",
  "description": "string"
}
```

## Başarılı response (200)

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "cashAccountId": "00000000-0000-0000-0000-000000000001",
  "bankAccountId": "00000000-0000-0000-0000-000000000001",
  "type": 1,
  "direction": 1,
  "amount": 1,
  "balanceAfter": 1,
  "currencyCode": "string",
  "transactionDate": "2026-07-29T12:00:00Z",
  "sourceType": 1,
  "sourceId": "00000000-0000-0000-0000-000000000001",
  "description": "string",
  "reversesTransactionId": "00000000-0000-0000-0000-000000000001",
  "createdBy": 1,
  "createdAt": "2026-07-29T12:00:00Z"
}
```

