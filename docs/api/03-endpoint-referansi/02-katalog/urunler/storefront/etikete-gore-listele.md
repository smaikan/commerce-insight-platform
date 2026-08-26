# GET /api/products/by-tag/{tagId}

- Görev alanı: **Katalog → Ürünler → Storefront**.
- İşlev: Seçilen etikete bağlı yayındaki ürünleri storefront kart sözleşmesiyle listeler.
- Yetki: **Public**.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `tagId` | path | Evet | string (uuid) |
| `PageNumber` | query | Hayır | integer (int32), varsayılan `1` |
| `PageSize` | query | Hayır | integer (int32), varsayılan `24`, en çok `100` |
| `SortBy` | query | Hayır | `Newest=0`, `Popularity=1`, `DisplayOrder=2`, `Title=3` |
| `Descending` | query | Hayır | boolean, varsayılan `true` |

## Başarılı response (200)

`PublishedProductListItemDtoPagedResult` döner. Yalnız `Status=Active` ve `IsActive=true` ürünler dahil edilir. Eşleşme yoksa boş `items` dizisi ve `totalCount: 0` döner.

Response alanları ve örnek gövde [yayımlanan ürünleri listeleme sözleşmesiyle](yayinlanan-urunleri-listele.md) aynıdır.
