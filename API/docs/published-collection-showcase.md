# Public koleksiyon vitrini

Storefront koleksiyon kartlarını tek sayfalı çağrıyla almak için anonim `GET /api/collections/published` endpointi kullanılır. Bu endpoint, genel yönetim/listeme sözleşmesi olan `GET /api/collections` veya filtre sözleşmesi olan koleksiyon facet endpointinin yerine geçmez; `/collections` vitrin sayfasının kart verisini toplu sağlar.

## Query parametreleri

| Parametre | Varsayılan | Kural |
| --- | ---: | --- |
| `PageNumber` | `1` | Pozitif tam sayı olmalıdır. |
| `PageSize` | `20` | `1..100` aralığında olmalıdır. |

Geçersiz query değerleri ortak `400 ProblemDetails` gövdesi üretir.

## Görünürlük ve sayma semantiği

Response yalnız aktif koleksiyonları ve aşağıdaki yayın koşullarını sağlayan en az bir ürünü içerir:

- Ürün soft-delete edilmemiştir.
- Ürün `IsActive=true` ve `Status=Active` durumundadır.
- `StoreSettings.showOutOfStockProducts=false` ise ürünün stokta olan aktif varyantı vardır.
- `StoreSettings.showProductsWithoutPrice=false` ise ürünün fiyat gösterebilecek aktif varyantı vardır.

`productCount`, aynı görünürlük kurallarını sağlayan benzersiz ürün-koleksiyon ilişkilerinin toplamıdır. Pasif, boş veya yalnız taslak/pasif/silinmiş ürün içeren koleksiyonlar response içinde bulunmaz. Filtreleme ve sayım sayfalama öncesinde SQL tarafında yapılır; `totalCount` yalnız uygun koleksiyonların toplamıdır.

## Sıralama ve etkili görsel

Koleksiyonlar `displayOrder` artan, `name` artan ve `id` artan sırasıyla döner. `imageUrl` aşağıdaki öncelikle backend tarafından üretilir:

1. Koleksiyonun kendi `imageUrl` değeri varsa aynen kullanılır.
2. Yoksa uygun ilk ürün `Product.DisplayOrder`, `Product.Title`, `Product.Id` artan sırasıyla seçilir. Bu ürünün görsellerinde `IsMain=true` önce, ardından `DisplayOrder` ve `Id` artan sırası kullanılır.
3. Koleksiyon görseli veya seçilen ürünün görseli yoksa açıkça `null` döner.

Frontend isimden slug üretmez; `url` alanını koleksiyon landing bağlantısı olarak kullanır. Frontend ayrıca görsel önceliğini yeniden hesaplamaz ve koleksiyon başına ürün isteği yapmaz.

## Response

Başarılı istek `200 OK` ve `PagedResult<PublishedCollectionShowcaseItemDto>` döndürür. `imageUrl` nullable, diğer kart alanları zorunludur.

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
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

Alan uzunlukları: `name` en fazla 150, `url` en fazla 200, `imageUrl` en fazla 500 karakterdir.

## Performans, cache ve invalidation

Tek HTTP isteği bütün sayfadaki koleksiyon kartlarını getirir. Tam HTTP akışı koleksiyon sayısından bağımsız üç SQL reader komutu çalıştırır: singleton StoreSettings okuması, filtrelenmiş `totalCount` ve sayfalı toplu kart projeksiyonu. Vitrin okuyucusunun kendi payı son iki komuttur; koleksiyon veya ürün başına ek sorgu çalışmaz.

Endpoint `public-products` output-cache politikasını kullanır:

- TTL 30 saniyedir.
- Cache anahtarı tüm query parametrelerine göre değişir.
- Kayıt ortak `products` etiketiyle işaretlenir.
- Ürün oluşturma/silme, yayın/aktivasyon ve ürün-koleksiyon ilişkisi değişiklikleri etiketi temizler.
- Ürün görseli oluşturma/güncelleme/silme etiketi temizler.
- Koleksiyon oluşturma/güncelleme/silme, aktivasyon, öne çıkarma ve koleksiyon görseli değişiklikleri etiketi temizler.
- Storefront görünürlük ayarı PUT ile değiştiğinde etiket temizlenir.

Bu nedenle vitrin, yayın ilişkisi veya etkili görseli değiştiren yönetim mutasyonlarından sonra eski TTL'yi beklemeden yenilenir.
