# GET /api/products/published/facets/product-types

- Görev alanı: **Katalog → Ürünler → Storefront → Filtre seçenekleri**.
- İşlev: Storefront ürün türü filtrelerini yayımlanmış ürün adetleriyle tek response içinde döndürür.
- Yetki: **Public**.
- Request body: Yok.

## Parametreler

`TypeId`, `BrandId`, `CollectionId` ve `TagId` opsiyonel, nullable UUID query parametreleridir. Bu endpoint ürün türü boyutunu hesapladığı için `TypeId` sayımdan çıkarılır; diğer gönderilen filtreler `AND` mantığıyla uygulanır.

Ayrıntılı görünürlük, öz-dışlama, cache ve frontend kullanım kuralları için [yayımlanmış ürün facet ortak sözleşmesine](ortak-sozlesme.md) bakın.

## Başarılı response (200)

`PublishedProductFacetItemDto[]` döner. Sıfır yayımlanmış ürünü olan veya pasif ürün türleri response içinde bulunmaz.

```json
[
  {
    "id": "33333333-3333-3333-3333-333333333333",
    "name": "Ürün türü",
    "productCount": 5
  }
]
```

Boş GUID filtresi `400 validation_error`; eşleşmeyen geçerli filtreler `200 OK` ve `[]` üretir.
