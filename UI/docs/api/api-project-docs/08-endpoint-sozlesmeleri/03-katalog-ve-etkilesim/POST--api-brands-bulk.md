# POST /api/brands/bulk

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Yeni kaynak veya iş akışı adımı oluşturur/başlatır.
- Operation ID: `POST-/api/brands/bulk`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `brands` | array | Evet |

```json
{
    "brands":  {
                   "name":  "string",
                   "url":  "string",
                   "description":  "string",
                   "isActive":  true
               }
}
```

## Başarılı response (200)

Response body yoktur.

