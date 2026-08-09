# POST /api/accounting/expenses/categories

- İşlev alanı: **10 Muhasebe alış ve gider**
- İşlev: Yeni kaynak veya iş akışı adımı oluşturur/başlatır.
- Operation ID: `POST-/api/accounting/expenses/categories`
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

```json
{
    "code":  "string",
    "name":  "string"
}
```

## Başarılı response (200)

```json
{
    "id":  "00000000-0000-0000-0000-000000000001",
    "code":  "string",
    "name":  "string",
    "isActive":  true
}
```

