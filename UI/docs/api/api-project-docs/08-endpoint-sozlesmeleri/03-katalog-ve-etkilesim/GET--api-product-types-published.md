# GET /api/product-types/published

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Storefront kategori kartlarını ürün adedi ve backend tarafından seçilen etkili görselle tek sayfalı response içinde döndürür.
- Yetki: `AllowAnonymous`; OpenAPI operation `security: []`.
- Cache: `public-products`, 30 saniye TTL ve `products` etiketi.

## Query

| Alan | Zorunlu | Varsayılan | Kural |
| --- | --- | ---: | --- |
| `PageNumber` | Hayır | `1` | Pozitif integer. |
| `PageSize` | Hayır | `20` | `1..100`. |

## Başarılı response

`200 PagedResult<PublishedProductTypeShowcaseItemDto>`:

```json
{
  "items": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "name": "Ayakkabı",
      "productCount": 12,
      "imageUrl": "https://cdn.example.com/categories/shoes.webp"
    },
    {
      "id": "22222222-2222-2222-2222-222222222222",
      "name": "Çanta",
      "productCount": 5,
      "imageUrl": "https://cdn.example.com/products/popular-bag.webp"
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 2,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

## Görünürlük ve etkili görsel

- Yalnız aktif ve en az bir görünür yayımlanmış ürünü bulunan ürün türleri döner.
- Soft-delete, ürün aktivasyonu, `ProductStatus.Active` ve StoreSettings stok/fiyat tercihleri hem `productCount` hem liste için SQL tarafında uygulanır.
- Kategoriler `name`, ardından `id` artan sıradadır; `totalCount` sayfalama öncesindeki uygun kategori toplamıdır.
- `imageUrl` önce ProductType kaydındaki özel görseldir.
- Özel görsel yoksa ürünler `PopularityScore DESC`, eşitlikte `Product.Id ASC` sırasıyla değerlendirilir; ilk ürünün görselinde `IsMain DESC`, `DisplayOrder ASC`, `Id ASC` kullanılır.
- Seçilen üründe görsel yoksa `imageUrl: null` döner. Frontend başka ürün aramak için kategori başına ek istek üretmez.

Tam HTTP akışı sabit üç reader komutudur: StoreSettings, filtrelenmiş `totalCount` ve sayfalı toplu kart projeksiyonu. Kategori sayısı arttığında sorgu sayısı artmaz.

Ürün yayın/aktivasyon, kategori ilişkisi, ürün görseli, ProductType veya StoreSettings görünürlük mutasyonları ortak `products` cache etiketini temizler. Geçersiz sayfalama ortak `400 ProblemDetails` cevabıdır.
