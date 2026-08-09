# POST /api/accounting/sales-invoices/{id}/cancel

- İşlev alanı: **09 Muhasebe satış ve fatura**
- İşlev: Kaynağı iptal eder veya muhasebe ters kaydını oluşturur.
- Operation ID: `POST-/api/accounting/sales-invoices/{id}/cancel`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `id` | path | Evet | string (uuid) |

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `reason` | string | Evet |

```json
{
    "reason":  "string"
}
```

## Başarılı response (200)

```json
{
    "id":  "00000000-0000-0000-0000-000000000001",
    "status":  "string",
    "alreadyProcessed":  true
}
```

