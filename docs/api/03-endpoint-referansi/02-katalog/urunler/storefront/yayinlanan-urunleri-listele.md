# GET /api/products/published

- Görev alanı: **Katalog → Ürünler → Storefront**.
- İşlev: Storefront için yalnız `Status=Active` ürünleri kart sözleşmesiyle listeler.
- Yetki: **Public**.

## Parametreler

| Ad | Konum | Zorunlu | Açıklama |
| --- | --- | --- | --- |
| `PageNumber` | query | Hayır | Varsayılan `1`. |
| `PageSize` | query | Hayır | Varsayılan `24`, en çok `100`. |
| `TypeId` | query | Hayır | Ürün türü GUID filtresi. |
| `BrandId` | query | Hayır | Marka GUID filtresi. |
| `CollectionId` | query | Hayır | Koleksiyon GUID filtresi. |
| `TagId` | query | Hayır | Etiket GUID filtresi. |
| `Search` | query | Hayır | Normalize edildikten sonra 2–100 karakter; başlık, marka, tür, koleksiyon, etiket ve MainSku üzerinde arama. |
| `SortBy` | query | Hayır | `Newest=0`, `Popularity=1`, `DisplayOrder=2`, `Title=3`. Gönderilmezse StoreSettings `defaultProductSort`. |
| `Descending` | query | Hayır | Gönderilmezse StoreSettings `defaultProductSortDescending`. |

## Başarılı response (200)

`PublishedProductListItemDtoPagedResult` döner. Her kart `id`, `title`, `url`, `summary`, `brandName`, en düşük aktif varyantın `price`/`compareAtPrice` değerleri, puan özeti, `mainImage`, `isAvailable`, `lowestAvailableStock` ve `isLowStock` alanlarını taşır.

```json
{
  "items": [
    {
      "id": "P0002F",
      "title": "Deri Omuz Çantası",
      "url": "deri-omuz-cantasi",
      "summary": "Günlük kullanıma uygun hakiki deri çanta.",
      "brandName": "Eleven",
      "price": 2499.9,
      "compareAtPrice": 2899.9,
      "averageRating": 4.8,
      "ratingCount": 42,
      "mainImage": {
        "id": "7badf50a-e661-4ef2-a344-84e54b6f7480",
        "productId": "P0002F",
        "imageUrl": "https://cdn.example.com/products/deri-omuz-cantasi.webp",
        "altText": "Kahverengi deri omuz çantası",
        "displayOrder": 0,
        "isMain": true
      },
      "isAvailable": true,
      "lowestAvailableStock": 3,
      "isLowStock": true
    }
  ],
  "pageNumber": 1,
  "pageSize": 24,
  "totalCount": 1,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

`isAvailable`, aktif varyantlardan en az birinin stoğu pozitifse true'dur. `lowestAvailableStock`, aktif ve stoğu pozitif varyantların minimumudur; toplam stok değildir ve uygun varyant yoksa null'dır. `isLowStock`, StoreSettings uyarısı açıkken bu varyantlardan en az birinin stoğu `1..lowStockThreshold` aralığındaysa true'dur.

Dört sınıflandırma filtresi birlikte gönderilebilir ve AND mantığıyla uygulanır. Endpoint her durumda yalnız `Status=Active` ve `IsActive=true` ürünleri döndürür. StoreSettings tercihlerine göre stoksuz ve/veya fiyat gösterebilen aktif varyantı olmayan ürünler SQL'de sayfalama öncesinde elenir; `totalCount` filtrelenmiş toplamdır. Boş GUID değerleri 400 validation hatasıdır; eşleşmeyen geçerli GUID boş sayfalı sonuç üretir. `showCompareAtPrice` görünüm tercihidir; API `compareAtPrice` verisini response'tan kaldırmaz.

`Search` doluysa sınıflandırma filtreleriyle AND çalışır ve count/items aynı arama filtresini kullanır. `SortBy` gönderilmezse exact başlık → başlık prefix → başlık contains → marka → tür → koleksiyon → etiket → popülerlik/displayOrder/id relevance sırası uygulanır. Explicit `SortBy` gönderilirse katalog sıralaması relevance'ı ezer. `Search` null veya boşsa endpointin önceki filtreleme ve StoreSettings varsayılan sıralama davranışı korunur. Count ve items toplam iki SQL komutudur; N+1 yoktur.
