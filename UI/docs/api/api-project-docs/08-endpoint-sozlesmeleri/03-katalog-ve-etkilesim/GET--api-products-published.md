# GET /api/products/published

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Storefront için yalnız `Status=Active` ürünleri kart sözleşmesiyle listeler.
- Yetki: `AllowAnonymous`.

## Parametreler

| Ad | Konum | Zorunlu | Açıklama |
| --- | --- | --- | --- |
| `PageNumber` | query | Hayır | Varsayılan `1`. |
| `PageSize` | query | Hayır | Varsayılan `24`, en çok `100`. |
| `SortBy` | query | Hayır | `Newest=0`, `Popularity=1`, `DisplayOrder=2`, `Title=3`. Varsayılan `Newest`. |
| `Descending` | query | Hayır | Varsayılan `true`; yeni ürünler önce gelir. |

## Başarılı response (200)

`PublishedProductListItemDtoPagedResult` döner. Her kart `id`, `title`, `url`, `summary`, `brandName`, en düşük aktif varyantın `price`/`compareAtPrice` değerleri, puan özeti ve `mainImage` alanlarını taşır.
