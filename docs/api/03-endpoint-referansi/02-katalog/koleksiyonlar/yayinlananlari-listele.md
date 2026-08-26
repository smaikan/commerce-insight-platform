# GET /api/collections/published

- Görev alanı: **Katalog → Koleksiyonlar**.
- İşlev: Storefront `/collections` kartlarını ürün adedi ve backend tarafından seçilmiş etkili görselle tek sayfalı response içinde döndürür.
- Yetki: **Public**.
- Request body: Yok.
- Cache: `public-products`, 30 saniye TTL, tüm query parametrelerine göre varyasyon, `products` etiketi.

## Query parametreleri

| Ad | Zorunlu | Varsayılan | Kural |
| --- | --- | ---: | --- |
| `PageNumber` | Hayır | `1` | Pozitif integer. |
| `PageSize` | Hayır | `20` | `1..100` aralığında integer. |

## Başarılı response (200)

`PagedResult<PublishedCollectionShowcaseItemDto>` döner.

```json
{
  "items": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "name": "Yaz Koleksiyonu",
      "url": "yaz-koleksiyonu",
      "productCount": 8,
      "isFeatured": true,
      "displayOrder": 1,
      "imageUrl": "https://cdn.example.com/collections/yaz.webp"
    },
    {
      "id": "22222222-2222-2222-2222-222222222222",
      "name": "Görselsiz Koleksiyon",
      "url": "gorselsiz-koleksiyon",
      "productCount": 2,
      "isFeatured": false,
      "displayOrder": 2,
      "imageUrl": null
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

Kart alanları:

| Alan | Nullable | Sınır / anlam |
| --- | --- | --- |
| `id` | Hayır | Koleksiyon UUID'si. |
| `name` | Hayır | En fazla 150 karakter. |
| `url` | Hayır | En fazla 200 karakter; landing bağlantısının backend kaynağıdır. |
| `productCount` | Hayır | Görünür yayımlanmış ürün ilişkilerinin toplamı. |
| `isFeatured` | Hayır | Koleksiyonun öne çıkarma işareti. |
| `displayOrder` | Hayır | Koleksiyonun manuel sırası. |
| `imageUrl` | Evet | En fazla 500 karakter; etkili vitrin görseli veya `null`. |

## Görünürlük, sıralama ve görsel önceliği

- Yalnız aktif ve en az bir görünür yayımlanmış ürünü bulunan koleksiyonlar döner.
- Ürün görünürlüğü soft-delete, `IsActive`, `Status=Active` ve StoreSettings stok/fiyat görünürlük tercihleriyle backend tarafından belirlenir.
- `productCount` ve `totalCount` sayfalama öncesinde aynı kurallarla hesaplanır.
- Koleksiyon sırası `displayOrder`, `name`, `id` artandır.
- `imageUrl` önce koleksiyonun kendi görselidir. Bu alan yoksa en popüler uygun ürün `popularityScore` azalan, eşitlikte ürün `id` artan sırasıyla seçilir; ürün görseli `isMain` önce, sonra `displayOrder` ve `id` sırasıyla seçilir.
- Koleksiyon ve seçilen ürün görseli yoksa `imageUrl: null` döner.

Storefront bu endpointi bir kez çağırmalı; koleksiyon başına `GET /api/products/published` isteği üretmemeli, isimden slug veya görsel fallback kuralı türetmemelidir.

Tam HTTP akışı sabit üç SQL reader komutudur: singleton StoreSettings, filtrelenmiş `totalCount` ve sayfalı toplu kart projeksiyonu. Koleksiyon sayısı arttığında sorgu sayısı artmaz.

## Hatalar ve cache invalidation

Geçersiz query değeri ortak `400 ProblemDetails` gövdesi üretir. Endpoint anonim olduğundan JWT gerekmez.

Ürün yayın/aktivasyon ve koleksiyon ilişkisi mutasyonları, ürün görseli mutasyonları, koleksiyon mutasyonları ve storefront görünürlük ayarı PUT işlemi ortak `products` cache etiketini temizler. Bu davranış koleksiyon görseli veya fallback ürün görseli değiştiğinde vitrin cevabını güncel tutar.
