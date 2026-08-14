# Katalog API'leri

Storefront katalog endpointleri Public; yönetim listesi ve yazma endpointleri Admin'dir. Product ID'si `P` prefix'li public ID, variant/image/relation ID'leri Guid'dir.

## Ürün

| Method | Endpoint | Yetki | Amaç |
| --- | --- | --- | --- |
| GET | `/api/products/published?Search=şönil&pageNumber=1&pageSize=24&typeId=...&brandId=...&collectionId=...&tagId=...&sortBy=0&descending=true` | Public | Arama ve taxonomy filtreli storefront ürün kartları |
| GET | `/api/products/published/search-suggestions?Query=şönil&Limit=10` | Public | Navbar için COUNT üretmeyen kompakt canlı ürün önerileri |
| GET | `/api/products/published/facets/brands?TypeId=...&BrandId=...&CollectionId=...&TagId=...` | Public | Marka seçenekleri ve yayımlanmış ürün adetleri; `BrandId` sayımdan dışlanır |
| GET | `/api/products/published/facets/collections?TypeId=...&BrandId=...&CollectionId=...&TagId=...` | Public | Koleksiyon seçenekleri ve yayımlanmış ürün adetleri; `CollectionId` sayımdan dışlanır |
| GET | `/api/products/published/facets/product-types?TypeId=...&BrandId=...&CollectionId=...&TagId=...` | Public | Ürün türü seçenekleri ve yayımlanmış ürün adetleri; `TypeId` sayımdan dışlanır |
| GET | `/api/collections/published?PageNumber=1&PageSize=20` | Public | Koleksiyon vitrin kartları; ürün adedi, canonical `url` ve etkili görsel tek sayfalı response içindedir |
| GET | `/api/products?pageNumber=1&pageSize=20&search=shirt&typeId=...&brandId=...&collectionId=...&tagId=...&status=...&isFeatured=false&sortBy=0&descending=true` | Admin | Filtrelenebilir operasyon ürün listesi |
| GET | `/api/products/by-collection/{collectionId}` | Public | Koleksiyondaki yayındaki ürünler |
| GET | `/api/products/by-tag/{tagId}` | Public | Etikete bağlı yayındaki ürünler |
| GET | `/api/products/by-type/{typeId}` | Public | Türdeki yayındaki ürünler |
| GET | `/api/products/by-brand/{brandId}` | Public | Markanın yayındaki ürünleri |
| GET | `/api/products/{productPublicId}` | Admin | Yönetim ürün detayı; storefront detay için `/api/products/by-url/{url}` kullanılır |
| POST | `/api/products` | Admin | Ürün ve opsiyonel varyantları atomik oluştur |
| POST | `/api/products/bulk` | Admin | Toplu ürün oluştur |
| DELETE | `/api/products/{productPublicId}` | Admin | Ürünü geçmişini koruyarak katalogdan kaldır (soft delete) |
| PUT | `/api/products/{productPublicId}` | Admin | Ana ürün alanları/etiketler |
| PATCH | `/api/products/{productPublicId}/status` | Admin | ProductStatus değiştir |
| PATCH | `/api/products/{productPublicId}/featured` | Admin | `{isFeatured}` |
| PUT | `/api/products/{productPublicId}/relations` | Admin | collection/tag/bundle ilişkileri |
| PATCH | `/api/products/{productPublicId}/has-variants` | Admin | Ürünün varyantlı/varyantsız sunum tercihini değiştir |

Ürün oluşturma:

```json
{
  "title": "Basic T-Shirt",
  "mainSku": "TSHIRT-MAIN-001",
  "typeId": null,
  "url": "basic-t-shirt",
  "brandId": null,
  "description": "Pamuklu tişört",
  "status": 1,
  "isFeatured": false,
  "displayOrder": 0,
  "seoTitle": "Basic T-Shirt",
  "seoDescription": "...",
  "collectionIds": [],
  "tags": ["basic", "cotton"],
  "taxRateId": null,
  "variants": [{ "name": "Default", "sku": "TSHIRT-001", "price": 499.90, "stock": 10, "compareAtPrice": null, "barcode": null, "material": "Cotton", "isActive": true }]
}
```

`ProductDto` response; `id` örneği `P00001`. Response ürün bilgileri, vergi/brand/type snapshot referansları, analytics counters, `variants`, `hasVariants` ve `tags` içerir. `hasVariants` kalıcı bir boolean alandır; create/bulk body içinde gönderilir, varsayılanı `false`tur ve `PATCH /api/products/{id}/has-variants` ile değiştirilebilir. Varyant sayısı birden fazlaysa değer `false` olamaz. `NetPrice` server tarafından türetilir; frontend göndermez.

## Varyantlı ve varyantsız ürün kararı

`hasVariants`, ürünün kaç `variants` kaydı bulunduğundan türetilmez. Admin bu alanı ürün oluştururken `POST /api/products` veya `POST /api/products/bulk` gövdesinde belirler; gönderilmezse başlangıç değeri `false` olur. Tek varyantlı ürün `false` seçildiğinde ana/tek ürün olarak listelenir; tek varyantlı ürün için `true` seçmek de geçerlidir. Birden fazla varyant varsa `hasVariants` zorunlu olarak `true` olmalıdır; `false` ile create, bulk create veya PATCH isteği 400 hatası döner. Daha sonra görünüm tercihi yalnız `PATCH /api/products/{id}/has-variants` ile değiştirilir.

Admin ürün listesi `Search`, `TypeId`, `BrandId`, `CollectionId`, `TagId`, `Status`, `IsActive`, `IsFeatured`, `SortBy` ve `Descending` filtrelerini destekler. Varsayılan sıralama `CreatedAt descending`dir; son eklenen ürün ilk gelir.

Storefront listesi `TypeId`, `BrandId`, `CollectionId` ve `TagId` filtrelerini destekler ve yalnız `Status=Active` ile `IsActive=true` ürünleri döner. Birden fazla sınıflandırma filtresi AND mantığıyla uygulanır. StoreSettings `showOutOfStockProducts` ve `showProductsWithoutPrice` tercihleri SQL'de sayfalama öncesinde uygulanır; `totalCount` doğru filtrelenmiş toplamdır. Ayrı `by-*` endpointleri aynı storefront kart sözleşmesini kullanır. `SortBy` seçenekleri `Newest=0`, `Popularity=1`, `DisplayOrder=2`, `Title=3`; query parametresi yoksa StoreSettings varsayılanı, açıkça gönderilmişse client seçimi uygulanır. Kart DTO'su `isAvailable`, varyant-bazlı `lowestAvailableStock` ve `isLowStock` alanlarını taşır.

Storefront filtre seçeneklerinin adetleri ürün listesinin mevcut sayfasından veya seçenek başına ek ürün isteğinden türetilmez. Marka, koleksiyon ve ürün türü facetleri yukarıdaki üç ayrı endpointten alınır. Her endpoint kendi boyut filtresini yok sayar, diğer boyutları AND mantığıyla uygular ve yalnız aktif, en az bir yayımlanmış ürünü bulunan seçenekleri `PublishedProductFacetItemDto[]` olarak döndürür. Ayrıntılar için [yayımlanmış ürün facet ortak sözleşmesine](../08-endpoint-sozlesmeleri/03-katalog-ve-etkilesim/YAYIMLANMIS-URUN-FACET-SOZLESMESI.md) bakın.

Public canlı arama başlık, marka, tür, koleksiyon, etiket ve MainSku alanlarını ortak normalize read model üzerinden arar. Çok kelimeli sorgular AND'dir. `Search` kullanılıp `SortBy` gönderilmezse relevance; explicit sıralama gönderilirse client tercihi uygulanır. Suggestion endpointi tek SQL ve `Limit+1` kullanır, `COUNT` üretmez, output cache'e alınmaz ve IP başına dakikada 120 istekle sınırlıdır. Ayrıntılı frontend sözleşmesi için [search suggestions belgesine](../08-endpoint-sozlesmeleri/03-katalog-ve-etkilesim/GET--api-products-published-search-suggestions.md) bakın.

## Varyant

| Method | Endpoint | Yetki | Body/amaç |
| --- | --- | --- | --- |
| GET | `/api/product-variants/{id}` | Public | Varyant detay |
| GET | `/api/product-variants/by-product/{productPublicId}?pageNumber=1&pageSize=20` | Public | Ürün varyantları |
| POST | `/api/product-variants/by-product/{productPublicId}` | Admin | Varyant + opening stock |
| PUT | `/api/product-variants/{id}` | Admin | Varyant alanları ve gerekirse stock count adjustment |
| PATCH | `/api/product-variants/{id}/price` | Admin | `{ "price": 499.9, "compareAtPrice": 599.9 }` |
| POST | `/api/product-variants/{id}/stock-movements` | Admin | Signed stok düzeltmesi |
| PATCH | `/api/product-variants/{id}/activation` | Admin | `{ "isActive": true }` |
| DELETE | `/api/product-variants/{id}` | Admin | History yoksa sil; history varsa engellenebilir |

Stok doğrudan güncellenmez; StockMovement ile değişir. `QuantityDelta` signed gönderilir. Ürün varyantı fiyatı KDV dahil katalog fiyatıdır.

## Marka, koleksiyon, ürün tipi, etiket

Her dört kaynakta ortak endpoint yapısı:

```text
GET    /api/brands                 GET /api/brands/{id}
POST   /api/brands                 POST /api/brands/bulk
PUT    /api/brands/{id}             PATCH /api/brands/{id}/activation
DELETE /api/brands/{id}
GET    /api/collections             GET /api/collections/{id}
GET    /api/collections/published
POST   /api/collections             POST /api/collections/bulk
PUT    /api/collections/{id}        PATCH /api/collections/{id}/activation
PATCH  /api/collections/{id}/featured
DELETE /api/collections/{id}
GET    /api/product-types           GET /api/product-types/{id}
POST   /api/product-types           POST /api/product-types/bulk
PUT    /api/product-types/{id}      PATCH /api/product-types/{id}/activation
DELETE /api/product-types/{id}
GET    /api/tags                    GET /api/tags/{id}
POST   /api/tags                    POST /api/tags/bulk
PUT    /api/tags/{id}               PATCH /api/tags/{id}/activation
DELETE /api/tags/{id}
```

Collection ayrıca `PATCH /api/collections/{id}/featured` kullanır. Her koleksiyon storefront kartı/başlığı için isteğe bağlı tek bir `imageUrl` taşır. Koleksiyon görseli zorunlu değildir; alan atlanır, `null` veya boş gönderilirse değer `null` olarak saklanır. Create body örneği:

```json
{ "name": "Yaz Koleksiyonu", "url": "yaz-koleksiyonu", "description": "...", "displayOrder": 1, "imageUrl": "https://cdn.example.com/collections/yaz.jpg" }
```

Storefront `/collections` sayfası genel `GET /api/collections` listesini, facet cevabını ve koleksiyon başına ürün sorgusunu birleştirmez. Bunun yerine `GET /api/collections/published` kullanır. Bu public sayfalı sözleşme yalnız aktif ve görünür yayımlanmış ürünü bulunan koleksiyonları; `id`, `name`, `url`, `productCount`, `isFeatured`, `displayOrder` ve nullable etkili `imageUrl` alanlarıyla döndürür. Görselde koleksiyon kaydı önceliklidir; yoksa backend'in kararlı sırasındaki ilk ürünün ana görseli, o da yoksa `null` döner. Ayrıntılar için [public koleksiyon vitrini sözleşmesine](../08-endpoint-sozlesmeleri/03-katalog-ve-etkilesim/GET--api-collections-published.md) bakın.

Brand body `{name,url,description,isActive,imageUrl}` biçimindedir. Marka `imageUrl` alanı da opsiyoneldir; alan atlanır, `null` veya boş gönderilirse değer `null` olarak saklanır. Marka ve koleksiyon görsel URL'leri en fazla 500 karakterdir. ProductType body `{name,description}`, Tag body `{name,url}`. GET listeleri paged DTO döner; yazma Admin'dir, okuma Public'dir.

Marka, koleksiyon, ürün türü ve etiket silme işlemleri kullanım durumundan bağımsız olarak `204 No Content` döner. Marka ve ürün türü silindiğinde ürün korunur, sırasıyla `brandId` ve `typeId` alanı `null` olur. Koleksiyon ve etiket silindiğinde ürün korunur; yalnız `ProductCollection` veya `ProductTag` bağlantı kaydı cascade olarak kaldırılır.

Ürün silme fiziksel değildir: ürün `Archived` ve pasif duruma alınır, katalog/admin/storefront okuma yollarından gizlenir. Sipariş, iade, stok, analitik ve muhasebe geçmişi korunur ve bu kayıtlar silmeyi engellemez. Tekrarlanan DELETE idempotenttir. Silinen ürünün ana SKU/URL değeri yeniden kullanılabilir; geçmiş varyantı korumak için varyant SKU değeri ayrılmış kalır.

## Storefront banner bölümleri

Bannerlar tek bir toplu kaynak değildir. Her biri en fazla 5 medya kaydı taşıyan altı bağımsız bölüm vardır:

| Bölüm | Public | Admin liste | Admin güncelleme |
| --- | --- | --- | --- |
| Main Banner | `GET /api/main-banners` | `GET /api/main-banners/admin` | `PUT /api/main-banners` |
| Alt Banner 1 | `GET /api/alt-banner-1` | `GET /api/alt-banner-1/admin` | `PUT /api/alt-banner-1` |
| Alt Banner 2 | `GET /api/alt-banner-2` | `GET /api/alt-banner-2/admin` | `PUT /api/alt-banner-2` |
| Alt Banner 3 | `GET /api/alt-banner-3` | `GET /api/alt-banner-3/admin` | `PUT /api/alt-banner-3` |
| Alt Banner 4 | `GET /api/alt-banner-4` | `GET /api/alt-banner-4/admin` | `PUT /api/alt-banner-4` |
| Alt Banner 5 | `GET /api/alt-banner-5` | `GET /api/alt-banner-5/admin` | `PUT /api/alt-banner-5` |

Public GET yalnız `isActive=true` kayıtları döndürür. `/admin` GET aktif ve pasif kayıtların tamamını döndürür. PUT yalnız kendi bölümünü atomik olarak değiştirir; `items: []` bölümü temizler ve diğer beş bölüme dokunmaz.

Her öğede `id`, `name`, `key`, `mediaUrl`, `mediaType`, `targetUrl`, `altText`, `displayOrder`, `isActive` ve `isMain` alanları bulunur. `mediaType`: `Image=1`, `Video=2`. `mediaUrl` mutlak HTTP/HTTPS adresidir; `targetUrl` uygulama içi `/...` yolu veya HTTP/HTTPS URL olabilir. `name` en fazla 150, `key` en fazla 100, URL ve alt metin alanları en fazla 500 karakterdir. Bölüm içinde `key` ve gönderilen `displayOrder` değerleri benzersiz olmalıdır.

Main Banner bölümü boş değilse tam olarak bir aktif kayıt `isMain=true` olmalıdır. Backend seçili kaydı otomatik olarak `displayOrder=0` konumuna taşır, kalan kayıtları gönderilen sıraya göre 1’den itibaren normalize eder. Alt Banner 1–5 bölümlerinde `isMain=true` geçersizdir. Hiçbir bölümü veya beş hakkın tamamını kullanmak zorunlu değildir.

## Ürün görselleri

| Method | Endpoint | Yetki |
| --- | --- | --- |
| GET | `/api/product-images/{id}` | Public |
| GET | `/api/product-images/by-product/{productPublicId}?pageNumber=1&pageSize=20` | Public |
| POST | `/api/product-images/by-product/{productPublicId}` | Admin |
| PUT | `/api/product-images/{id}` | Admin |
| DELETE | `/api/product-images/{id}` | Admin |

Body:

```json
{ "imageUrl": "https://cdn.example.com/item.jpg", "altText": "Önden görünüm", "displayOrder": 0, "isMain": true }
```

## Ürün etkileşimleri

| Method | Endpoint | Yetki | Body |
| --- | --- | --- | --- |
| GET | `/api/product-engagement/favorites?pageNumber=1&pageSize=20` | Public/User | JWT user veya ortak guest session favorileri |
| POST | `/api/product-engagement/products/{productPublicId}/favorites` | Public/User | body yok; guest için Origin + `X-Guest-CSRF` |
| DELETE | `/api/product-engagement/products/{productPublicId}/favorites` | Public/User | body yok; guest için Origin + `X-Guest-CSRF` |
| PUT | `/api/product-engagement/products/{productPublicId}/rating` | User | `{ "ratingValue": 1..5 }` |
| POST | `/api/product-engagement/products/{productPublicId}/reviews` | User | `{ "comment": "...", "title": "...", "ratingValue": 1..5 }` |
| GET | `/api/product-engagement/products/{productPublicId}/reviews?pageNumber=1&pageSize=20` | Public | Onaylı yorumlar |
| PATCH | `/api/product-engagement/reviews/{reviewId}/approval` | Admin | `{ "isApproved": true }` |
| GET | `/api/product-engagement/products/{productPublicId}/metrics?from=2026-07-01&to=2026-07-31` | Admin | Günlük metrikler |
| POST | `/api/product-engagement/products/{productPublicId}/activities` | User | `{ "activityType": 0, "productVariantId": "guid", "quantity": 1 }` |

Activity enum: `0 Click`, `1 AddToCart`, `2 Purchase`. Add-to-cart/purchase counters güvenilir Cart/Order akışlarından da güncellenir; frontend aynı olayı iki kez göndermemelidir.

Guest favorites ve guest cart aynı `ecommerce_guest_cart` HttpOnly cookie'sini (`Path=/api`) kullanır. JWT varsa guest cookie yok sayılır. Guest mutationlarda BFF cookie tokenını sunucu tarafında `X-Guest-CSRF` header'ına kopyalar ve trusted `Origin` iletir; token client JavaScript, log veya analytics'e açılmaz. Login sonrasında `/api/guest-session/claim`, iki veri alanını tek transaction ile ve union yapmadan öncelik kurallarına göre claim eder.
