# POST /api/accounting/cash-accounts

- İşlev alanı: **11 Muhasebe tahsilat, kasa ve banka**
- İşlev: Yeni kaynak veya iş akışı adımı oluşturur/başlatır.
- Operation ID: `POST-/api/accounting/cash-accounts`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `code` | string | Evet |
| `name` | string | Evet |
| `currencyCode` | string | Evet |

```json
{
    "code":  "string",
    "name":  "string",
    "currencyCode":  "string"
}
```

## Başarılı response (200)

```json
{
    "id":  "00000000-0000-0000-0000-000000000001",
    "code":  "string",
    "name":  "string",
    "currencyCode":  "string",
    "isActive":  true,
    "balance":  1
}
```

