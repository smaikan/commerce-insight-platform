# GET /api/products

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Kaynağı veya filtrelenmiş kaynak listesini okur.
- Operation ID: `GET-/api/products`
- Yetki: `AdminOnly`.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `PageNumber` | query | Hayır | integer (int32) |
| `PageSize` | query | Hayır | integer (int32) |
| `Search` | query | Hayır | string |
| `TypeId` | query | Hayır | string (uuid) |
| `BrandId` | query | Hayır | string (uuid) |
| `Status` | query | Hayır |  |
| `IsFeatured` | query | Hayır | boolean |
| `SortBy` | query | Hayır |  |
| `Descending` | query | Hayır | boolean |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

`ProductDtoPagedResult` döner. Her liste öğesindeki `mainImage`, backend'in `isMain` önceliğiyle seçtiği tek liste görselidir; frontend ürün başına ek görsel isteği yapmaz. Ürün aktifliği yalnız `status` alanından yönetilir; yalnız `Active` durumundaki ürün satışa açıktır.

