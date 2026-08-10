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
| `CollectionId` | query | Hayır | string (uuid) |
| `TagId` | query | Hayır | string (uuid) |
| `Status` | query | Hayır |  |
| `IsActive` | query | Hayır | boolean |
| `IsFeatured` | query | Hayır | boolean |
| `SortBy` | query | Hayır |  |
| `Descending` | query | Hayır | boolean |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

`ProductDtoPagedResult` döner. Her liste öğesindeki `mainImage`, backend'in `isMain` önceliğiyle seçtiği tek liste görselidir; frontend ürün başına ek görsel isteği yapmaz. Mevcut runtime sözleşmesinde `Status` ve `IsActive` alanları birlikte bulunur.

`TypeId`, `BrandId`, `CollectionId` ve `TagId` filtreleri birlikte gönderilebilir. Birden fazla filtre gönderildiğinde backend koşulları AND mantığıyla uygular. Boş GUID değerleri 400 validation hatasıdır.

