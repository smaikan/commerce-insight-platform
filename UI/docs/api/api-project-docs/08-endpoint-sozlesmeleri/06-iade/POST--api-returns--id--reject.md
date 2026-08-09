# POST /api/returns/{id}/reject

- İşlev alanı: **06 İade**
- İşlev: Bekleyen iade talebini reddeder.
- Operation ID: `POST-/api/returns/{id}/reject`
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
| `decisionNote` | string | Hayır |

```json
{
    "decisionNote":  "string"
}
```

## Başarılı response (200)

```json
{
    "id":  "00000000-0000-0000-0000-000000000001",
    "returnNumber":  "string",
    "orderId":  "00000000-0000-0000-0000-000000000001",
    "type":  0,
    "status":  0,
    "refundTotal":  1,
    "customerNote":  "string",
    "decisionNote":  "string",
    "items":  {
                  "id":  "00000000-0000-0000-0000-000000000001",
                  "orderItemId":  "00000000-0000-0000-0000-000000000001",
                  "productId":  "string",
                  "productVariantId":  "00000000-0000-0000-0000-000000000001",
                  "productTitle":  "string",
                  "variantSku":  "string",
                  "unitPrice":  1,
                  "quantity":  1,
                  "lineTotal":  1,
                  "refundTotal":  1,
                  "replacementProductVariantId":  "00000000-0000-0000-0000-000000000001"
              },
    "approvedAt":  "2026-07-29T12:00:00Z",
    "rejectedAt":  "2026-07-29T12:00:00Z",
    "receivedAt":  "2026-07-29T12:00:00Z",
    "completedAt":  "2026-07-29T12:00:00Z",
    "createdAt":  "2026-07-29T12:00:00Z"
}
```

