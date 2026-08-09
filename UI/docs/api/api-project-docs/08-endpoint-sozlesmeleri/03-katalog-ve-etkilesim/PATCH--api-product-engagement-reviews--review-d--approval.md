# PATCH /api/product-engagement/reviews/{reviewId}/approval

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Kaynağın belirli durum veya alanlarını değiştirir.
- Operation ID: `PATCH-/api/product-engagement/reviews/{reviewId}/approval`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `reviewId` | path | Evet | string (uuid) |

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `isApproved` | boolean | Evet |

```json
{
    "isApproved":  true
}
```

## Başarılı response (200)

Response body yoktur.

