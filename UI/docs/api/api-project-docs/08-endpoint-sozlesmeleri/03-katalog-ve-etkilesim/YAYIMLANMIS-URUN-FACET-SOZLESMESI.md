# Yayımlanmış ürün facet ortak sözleşmesi

Storefront, yayımlanmış katalogdaki marka, koleksiyon ve ürün türü filtrelerini ürün adetleriyle üç ayrı public endpointten alır:

- `GET /api/products/published/facets/brands`
- `GET /api/products/published/facets/collections`
- `GET /api/products/published/facets/product-types`

Her endpoint kendi facet boyutunun tüm uygun seçeneklerini tek response içinde döndürür. Frontend seçenek başına `GET /api/products/published` isteği yapmamalıdır.

## Ortak query sözleşmesi

| Ad | Tip | Zorunlu | Nullable | Açıklama |
| --- | --- | --- | --- | --- |
| `TypeId` | UUID | Hayır | Evet | Ürün türü filtresi. `product-types` endpointinde öz-dışlama nedeniyle sayımdan çıkarılır. |
| `BrandId` | UUID | Hayır | Evet | Marka filtresi. `brands` endpointinde öz-dışlama nedeniyle sayımdan çıkarılır. |
| `CollectionId` | UUID | Hayır | Evet | Koleksiyon filtresi. `collections` endpointinde öz-dışlama nedeniyle sayımdan çıkarılır. |
| `TagId` | UUID | Hayır | Evet | Etiket filtresi; üç endpointte de uygulanır. |

Parametreler gönderilmediğinde ilgili filtre uygulanmaz. Boş GUID (`00000000-0000-0000-0000-000000000000`) geçersizdir ve `400 validation_error` döner.

## Öz-dışlama ve AND semantiği

Her endpoint kendi boyutunun query değerini bilerek yok sayar, diğer boyutları `AND` mantığıyla uygular. Böylece kullanıcı bir marka seçtiğinde marka facet listesi yalnız seçili markaya daralıp alternatifleri tamamen kaybetmez.

Örnek: `brands?TypeId={typeId}&BrandId={selectedBrandId}&CollectionId={collectionId}` isteğinde `BrandId` sayımdan çıkarılır; `TypeId` ve `CollectionId` uygulanır. Aynı davranış sırasıyla koleksiyon ve ürün türü endpointlerinin kendi boyutları için geçerlidir.

## Görünürlük ve sayım

Sayıma yalnız aşağıdaki ürünler dahil edilir:

- Soft-delete edilmemiş (`DeletedAtUtc == null`).
- Aktif (`IsActive == true`).
- Yayın durumunda (`Status == Active`).

Dönen facet kaydı da aktif olmalıdır. Eşleşen yayımlanmış ürün adedi sıfır olan marka, koleksiyon veya ürün türü response içinde yer almaz. Sonuç sayfalama uygulanmadan toplam adetleri içerir. Koleksiyon adetleri benzersiz ürün-koleksiyon ilişkilerinden hesaplanır.

## Response modeli

Başarılı response `200 OK` ve `PublishedProductFacetItemDto[]` döner.

| Alan | Tip | Required | Nullable | Açıklama |
| --- | --- | --- | --- | --- |
| `id` | UUID | Evet | Hayır | Marka, koleksiyon veya ürün türü kimliği. |
| `name` | string | Evet | Hayır | Kullanıcıya gösterilecek sınıflandırma adı. |
| `productCount` | int32 | Evet | Hayır | Diğer seçili filtreler uygulandıktan sonraki toplam yayımlanmış ürün adedi. |

```json
[
  {
    "id": "11111111-1111-1111-1111-111111111111",
    "name": "Marka",
    "productCount": 12
  }
]
```

Eşleşme yoksa `200 OK` ile boş dizi (`[]`) döner.

## Frontend kullanım kararı

- Ürün kartları `GET /api/products/published` üzerinden sayfalı alınır.
- Filtre seçenekleri ve adetleri üç facet endpointinden, her boyut için tek istekle alınır.
- Facet sonuçları sayfalı ürün listesinin yalnız mevcut sayfasından türetilmez.
- Aynı filtre durumu üç endpointte de gönderilir; backend öz-dışlama semantiğini uygular.
- Response sırası backend tarafından ada, ardından kimliğe göre kararlı üretilir; UI gerekmedikçe yeniden sıralamamalıdır.

## Cache ve invalidation

Endpointler `public-products` output-cache politikasını kullanır. TTL 30 saniyedir ve cache anahtarı tüm query parametrelerine göre değişir. Ürün yayınlama/pasife alma, ürün-sınıflandırma ilişki değişiklikleri ile marka, koleksiyon, ürün türü ve etiket mutasyonları ortak `products` cache etiketini geçersiz kılar.
