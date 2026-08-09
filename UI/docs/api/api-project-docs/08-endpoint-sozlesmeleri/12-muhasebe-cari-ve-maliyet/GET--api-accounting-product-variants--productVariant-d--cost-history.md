# GET /api/accounting/product-variants/{productVariantId}/cost-history

- İşlev alanı: **12 Muhasebe cari ve maliyet**
- İşlev: Kaynağı veya filtrelenmiş kaynak listesini okur.
- Operation ID: `GET-/api/accounting/product-variants/{productVariantId}/cost-history`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `productVariantId` | path | Evet | string (uuid) |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

```json
{
    "id":  "00000000-0000-0000-0000-000000000001",
    "productVariantId":  "00000000-0000-0000-0000-000000000001",
    "sourceType":  1,
    "sourceId":  "00000000-0000-0000-0000-000000000001",
    "previousCostExcludingVat":  1,
    "newCostExcludingVat":  1,
    "previousCostIncludingVat":  1,
    "newCostIncludingVat":  1,
    "validFrom":  "2026-07-29T12:00:00Z",
    "validTo":  "2026-07-29T12:00:00Z",
    "openingStockQuantity":  1,
    "closingStockQuantity":  1,
    "createdAt":  "2026-07-29T12:00:00Z"
}
```

