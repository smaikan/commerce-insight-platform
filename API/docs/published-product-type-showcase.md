# Public kategori vitrini

Projede ana kategori kavramı `ProductType`tır. Storefront kategori kartları anonim `GET /api/product-types/published` endpointinden tek sayfalı çağrıyla alınır. Genel `GET /api/product-types` kalıcı yönetim alanlarını, published endpoint ise yayımlanmış ürün görünürlüğüyle hesaplanan etkili kart görselini döndürür.

## Kalıcı görsel

ProductType nullable `imageUrl` taşır. Create, update ve bulk create requestlerinde alan en fazla 500 karakterdir. Boş değer `null` olarak saklanır. API dosya yüklemez; admin tarafından yüklenmiş CDN URL'sini saklar.

## Public response ve fallback

`PagedResult<PublishedProductTypeShowcaseItemDto>` içindeki her kart `id`, `name`, `productCount` ve nullable `imageUrl` taşır. Yalnız aktif ve en az bir görünür yayımlanmış ürünü bulunan türler döner. StoreSettings stok/fiyat tercihleri sayım ve sayfalama öncesinde SQL'e uygulanır.

Etkili `imageUrl` önceliği:

1. ProductType özel `imageUrl` değeri.
2. `PopularityScore DESC`, eşitlikte `Product.Id ASC` sırasındaki ilk görünür yayımlanmış ürün.
3. Seçilen ürünün `IsMain DESC`, `DisplayOrder ASC`, `Id ASC` sırasındaki görseli.
4. Özel veya ürün görseli yoksa `null`.

Kategori listesi `Name ASC`, `Id ASC` sırasındadır. Endpoint StoreSettings, count ve toplu projection olmak üzere sabit üç reader komutu çalıştırır; kategori veya ürün başına sorgu üretmez.

## Cache

Endpoint 30 saniyelik `public-products` output-cache politikasını ve `products` etiketini kullanır. Ürün, ürün görseli, kategori ilişkisi, ProductType veya StoreSettings görünürlük mutasyonları etiketi temizler. Frontend kategori başına ürün isteği veya kendi görsel fallback kuralını üretmemelidir.
