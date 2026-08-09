# POST /api/orders/reservations/expire

- İşlev alanı: **05 Sipariş ve ödeme**
- İşlev: Yeni kaynak veya iş akışı adımı oluşturur/başlatır.
- Operation ID: `POST-/api/orders/reservations/expire`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `batchSize` | integer (int32) | Evet |

```json
{
    "batchSize":  1
}
```

## Başarılı response (200)

```json
{
    "cancelledOrderCount":  1,
    "skippedPendingPaymentCount":  1,
    "reconciledPaidOrderCount":  1
}
```

