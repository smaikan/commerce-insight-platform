# GET /api/products/published/facets/brands

- Görev alanı: **Katalog → Ürünler → Storefront → Filtre seçenekleri**.
- İşlev: Storefront marka filtrelerini yayımlanmış ürün adetleriyle tek response içinde döndürür.
- Yetki: **Public**.
- Request body: Yok.

## Parametreler

`TypeId`, `BrandId`, `CollectionId` ve `TagId` opsiyonel, nullable UUID query parametreleridir. Bu endpoint marka boyutunu hesapladığı için `BrandId` sayımdan çıkarılır; diğer gönderilen filtreler `AND` mantığıyla uygulanır.

Ayrıntılı görünürlük, öz-dışlama, cache ve frontend kullanım kuralları için [yayımlanmış ürün facet ortak sözleşmesine](ortak-sozlesme.md) bakın.

## Başarılı response (200)

`PublishedProductFacetItemDto[]` döner. Sıfır yayımlanmış ürünü olan veya pasif markalar response içinde bulunmaz.

```json
[
  {
    "id": "11111111-1111-1111-1111-111111111111",
    "name": "Marka",
    "productCount": 12
  }
]
```

Boş GUID filtresi `400 validation_error`; eşleşmeyen geçerli filtreler `200 OK` ve `[]` üretir.
