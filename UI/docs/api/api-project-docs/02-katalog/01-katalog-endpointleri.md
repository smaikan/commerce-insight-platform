# Katalog API'leri

Storefront katalog endpointleri Public; yönetim listesi ve yazma endpointleri Admin'dir. Product ID'si `P` prefix'li public ID, variant/image/relation ID'leri Guid'dir.

## Ürün

| Method | Endpoint | Yetki | Amaç |
| --- | --- | --- | --- |
| GET | `/api/products/published?pageNumber=1&pageSize=24&sortBy=0&descending=true` | Public | Storefront ürün kartları |
| GET | `/api/products?pageNumber=1&pageSize=20&search=shirt&typeId=...&brandId=...&status=...&isFeatured=false&sortBy=0&descending=true` | Admin | Operasyon ürün listesi |
| GET | `/api/products/{productPublicId}` | Public | Ürün detay |
| POST | `/api/products` | Admin | Ürün ve opsiyonel varyantları atomik oluştur |
| POST | `/api/products/bulk` | Admin | Toplu ürün oluştur |
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

Admin ürün listesi `Search`, `TypeId`, `BrandId`, `Status`, `IsFeatured`, `SortBy` ve `Descending` filtrelerini destekler. Varsayılan sıralama `CreatedAt descending`dir; son eklenen ürün ilk gelir.

Storefront listesi yalnız `Status=Active` ürünleri döner. `SortBy` seçenekleri `Newest=0`, `Popularity=1`, `DisplayOrder=2` ve `Title=3` değerleridir. Varsayılan `Newest` + `Descending=true` olduğundan son eklenen ürün ilk gelir.

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
GET    /api/collections             GET /api/collections/{id}
POST   /api/collections             POST /api/collections/bulk
PUT    /api/collections/{id}        PATCH /api/collections/{id}/activation
PATCH  /api/collections/{id}/featured
GET    /api/product-types           GET /api/product-types/{id}
POST   /api/product-types           POST /api/product-types/bulk
PUT    /api/product-types/{id}      PATCH /api/product-types/{id}/activation
GET    /api/tags                    GET /api/tags/{id}
POST   /api/tags                    POST /api/tags/bulk
PUT    /api/tags/{id}               PATCH /api/tags/{id}/activation
```

Collection ayrıca `PATCH /api/collections/{id}/featured` kullanır. Create body örneği:

```json
{ "name": "Yaz Koleksiyonu", "url": "yaz-koleksiyonu", "description": "...", "displayOrder": 1 }
```

Brand body `{name,url,description}`, ProductType body `{name,description}`, Tag body `{name,url}`. GET listeleri paged DTO döner; yazma Admin'dir, okuma Public'dir.

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
| GET | `/api/product-engagement/favorites?pageNumber=1&pageSize=20` | User | Kullanıcının favorileri |
| POST | `/api/product-engagement/products/{productPublicId}/favorites` | User | body yok |
| DELETE | `/api/product-engagement/products/{productPublicId}/favorites` | User | body yok |
| PUT | `/api/product-engagement/products/{productPublicId}/rating` | User | `{ "ratingValue": 1..5 }` |
| POST | `/api/product-engagement/products/{productPublicId}/reviews` | User | `{ "comment": "...", "title": "...", "ratingValue": 1..5 }` |
| GET | `/api/product-engagement/products/{productPublicId}/reviews?pageNumber=1&pageSize=20` | Public | Onaylı yorumlar |
| PATCH | `/api/product-engagement/reviews/{reviewId}/approval` | Admin | `{ "isApproved": true }` |
| GET | `/api/product-engagement/products/{productPublicId}/metrics?from=2026-07-01&to=2026-07-31` | Admin | Günlük metrikler |
| POST | `/api/product-engagement/products/{productPublicId}/activities` | User | `{ "activityType": 0, "productVariantId": "guid", "quantity": 1 }` |

Activity enum: `0 Click`, `1 AddToCart`, `2 Purchase`. Add-to-cart/purchase counters güvenilir Cart/Order akışlarından da güncellenir; frontend aynı olayı iki kez göndermemelidir.
