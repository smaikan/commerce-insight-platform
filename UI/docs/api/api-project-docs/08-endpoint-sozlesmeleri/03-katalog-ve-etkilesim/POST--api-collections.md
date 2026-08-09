# POST /api/collections

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Yeni kaynak veya iş akışı adımı oluşturur/başlatır.
- Operation ID: `POST-/api/collections`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `name` | string | Evet |
| `url` | string | Hayır |
| `description` | string | Hayır |
| `isActive` | boolean | Evet |
| `isFeatured` | boolean | Evet |
| `displayOrder` | integer (int32) | Evet |

```json
{
    "name":  "string",
    "url":  "string",
    "description":  "string",
    "isActive":  true,
    "isFeatured":  true,
    "displayOrder":  1
}
```

## Başarılı response (200)

```json
{
    "id":  "00000000-0000-0000-0000-000000000001",
    "name":  "string",
    "description":  "string",
    "url":  "string",
    "isActive":  true,
    "isFeatured":  true,
    "displayOrder":  1
}
```

