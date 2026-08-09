# POST /api/accounting/expenses

- İşlev alanı: **10 Muhasebe alış ve gider**
- İşlev: Yeni kaynak veya iş akışı adımı oluşturur/başlatır.
- Operation ID: `POST-/api/accounting/expenses`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `categoryId` | string (uuid) | Evet |
| `amountExcludingVat` | number (double) | Evet |
| `vatRate` | number (double) | Evet |
| `expenseDate` | string (date-time) | Evet |
| `description` | string | Evet |

```json
{
    "categoryId":  "00000000-0000-0000-0000-000000000001",
    "amountExcludingVat":  1,
    "vatRate":  1,
    "expenseDate":  "2026-07-29T12:00:00Z",
    "description":  "string"
}
```

## Başarılı response (200)

```json
{
    "id":  "00000000-0000-0000-0000-000000000001",
    "categoryId":  "00000000-0000-0000-0000-000000000001",
    "type":  1,
    "amountExcludingVat":  1,
    "vatRate":  1,
    "vatAmount":  1,
    "totalAmountIncludingVat":  1,
    "expenseDate":  "2026-07-29T12:00:00Z",
    "description":  "string"
}
```

