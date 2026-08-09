# POST /api/stock-movements/bulk

- İşlev alanı: **07 Yönetim, stok ve kampanya**
- İşlev: Yeni kaynak veya iş akışı adımı oluşturur/başlatır.
- Operation ID: `POST-/api/stock-movements/bulk`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `movements` | array | Evet |

```json
{
    "movements":  {
                      "productVariantId":  "00000000-0000-0000-0000-000000000001",
                      "quantityDelta":  1,
                      "type":  1,
                      "reason":  "string"
                  }
}
```

## Başarılı response (200)

```json
{
    "movementCount":  1,
    "movements":  {
                      "id":  "00000000-0000-0000-0000-000000000001",
                      "productVariantId":  "00000000-0000-0000-0000-000000000001",
                      "direction":  1,
                      "type":  1,
                      "quantityDelta":  1,
                      "stockBeforeMovement":  1,
                      "stockAfterMovement":  1,
                      "reason":  "string",
                      "orderId":  "00000000-0000-0000-0000-000000000001",
                      "returnRequestId":  "00000000-0000-0000-0000-000000000001",
                      "createdAt":  "2026-07-29T12:00:00Z"
                  }
}
```

