# PATCH /api/accounting/inventory-cost-layers/{id}/opening-balance-cost

- Görev alanı: **Muhasebe → Maliyet → Açılış maliyetleri**.
- İşlev: Açılış maliyetini günceller.
- Operation ID: `PATCH-/api/accounting/inventory-cost-layers/{id}/opening-balance-cost`
- Yetki: **Admin**.
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
| `expectedConcurrencyToken` | string (uuid) | Evet |
| `unitCostExcludingVat` | number (double) | Evet |
| `unitCostIncludingVat` | number (double) | Hayır |

```json
{
  "expectedConcurrencyToken": "00000000-0000-0000-0000-000000000001",
  "unitCostExcludingVat": 1,
  "unitCostIncludingVat": 1
}
```

## Başarılı response (200)

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "productVariantId": "00000000-0000-0000-0000-000000000001",
  "stockMovementId": "00000000-0000-0000-0000-000000000001",
  "sourceType": 1,
  "originalQuantity": 1,
  "remainingQuantity": 1,
  "unitCostExcludingVat": 1,
  "unitCostIncludingVat": 1,
  "totalCostExcludingVat": 1,
  "totalCostIncludingVat": 1,
  "costDate": "2026-07-29T12:00:00Z",
  "status": 1,
  "concurrencyToken": "00000000-0000-0000-0000-000000000001"
}
```

