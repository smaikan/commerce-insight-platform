# GET /api/stock-movements

- İşlev alanı: **07 Yönetim, stok ve kampanya**
- İşlev: Kaynağı veya filtrelenmiş kaynak listesini okur.
- Operation ID: `GET-/api/stock-movements`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `PageNumber` | query | Hayır | integer (int32) |
| `PageSize` | query | Hayır | integer (int32) |
| `ProductVariantId` | query | Hayır | string (uuid) |
| `Direction` | query | Hayır |  |
| `Type` | query | Hayır |  |
| `CreatedFromUtc` | query | Hayır | string (date-time) |
| `CreatedToUtc` | query | Hayır | string (date-time) |
| `Search` | query | Hayır | string; ürün başlığı, varyant adı/değeri veya SKU |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

```json
{
    "items":  {
                  "id":  "00000000-0000-0000-0000-000000000001",
                  "productVariantId":  "00000000-0000-0000-0000-000000000001",
                  "productTitle": "Gümüş Kolye",
                  "variantName": "Renk / Boyut",
                  "variantValue": "Gümüş / Mini",
                  "sku": "KOLYE-GUMUS-MINI",
                  "direction":  1,
                  "type":  1,
                  "quantityDelta":  1,
                  "stockBeforeMovement":  1,
                  "stockAfterMovement":  1,
                  "reason":  "string",
                  "orderId":  "00000000-0000-0000-0000-000000000001",
                  "returnRequestId":  "00000000-0000-0000-0000-000000000001",
                  "createdAt":  "2026-07-29T12:00:00Z"
              },
    "pageNumber":  1,
    "pageSize":  1,
    "totalCount":  1,
    "totalPages":  1,
    "hasPreviousPage":  true,
    "hasNextPage":  true
}
```

