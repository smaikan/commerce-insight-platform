# GET /api/products/published/facets/collections

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Storefront koleksiyon filtrelerini yayımlanmış ürün adetleriyle tek response içinde döndürür.
- Yetki: `AllowAnonymous` (Public).
- Request body: Yok.

## Parametreler

`TypeId`, `BrandId`, `CollectionId` ve `TagId` opsiyonel, nullable UUID query parametreleridir. Bu endpoint koleksiyon boyutunu hesapladığı için `CollectionId` sayımdan çıkarılır; diğer gönderilen filtreler `AND` mantığıyla uygulanır.

Ayrıntılı görünürlük, öz-dışlama, cache ve frontend kullanım kuralları için [yayımlanmış ürün facet ortak sözleşmesine](YAYIMLANMIS-URUN-FACET-SOZLESMESI.md) bakın.

## Başarılı response (200)

`PublishedProductFacetItemDto[]` döner. Sıfır yayımlanmış ürünü olan veya pasif koleksiyonlar response içinde bulunmaz.

```json
[
  {
    "id": "22222222-2222-2222-2222-222222222222",
    "name": "Koleksiyon",
    "productCount": 8
  }
]
```

Boş GUID filtresi `400 validation_error`; eşleşmeyen geçerli filtreler `200 OK` ve `[]` üretir.
