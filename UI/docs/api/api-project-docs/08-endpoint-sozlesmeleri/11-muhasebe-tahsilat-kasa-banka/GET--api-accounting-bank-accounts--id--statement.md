# GET /api/accounting/bank-accounts/{id}/statement

- İşlev alanı: **11 Muhasebe tahsilat, kasa ve banka**
- İşlev: Kaynağı veya filtrelenmiş kaynak listesini okur.
- Operation ID: `GET-/api/accounting/bank-accounts/{id}/statement`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `id` | path | Evet | string (uuid) |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

```json
{
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
```

