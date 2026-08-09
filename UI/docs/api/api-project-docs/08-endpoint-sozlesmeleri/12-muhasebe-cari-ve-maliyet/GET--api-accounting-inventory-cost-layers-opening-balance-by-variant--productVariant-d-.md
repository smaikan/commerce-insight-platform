# GET /api/accounting/inventory-cost-layers/opening-balance/by-variant/{productVariantId}

- İşlev alanı: **12 Muhasebe cari ve maliyet**
- İşlev: Kaynağı veya filtrelenmiş kaynak listesini okur.
- Operation ID: `GET-/api/accounting/inventory-cost-layers/opening-balance/by-variant/{productVariantId}`
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
    "stockMovementId":  "00000000-0000-0000-0000-000000000001",
    "sourceType":  1,
    "originalQuantity":  1,
    "remainingQuantity":  1,
    "unitCostExcludingVat":  1,
    "unitCostIncludingVat":  1,
    "totalCostExcludingVat":  1,
    "totalCostIncludingVat":  1,
    "costDate":  "2026-07-29T12:00:00Z",
    "status":  1,
    "concurrencyToken":  "00000000-0000-0000-0000-000000000001"
}
```

