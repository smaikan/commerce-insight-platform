# Yayımlanmış ürün facet endpointleri

Storefront marka, koleksiyon ve ürün türü filtrelerini ürün adetleriyle almak için üç ayrı anonim endpoint kullanır:

- `GET /api/products/published/facets/brands`
- `GET /api/products/published/facets/collections`
- `GET /api/products/published/facets/product-types`

Her endpoint tek veritabanı sorgusu çalıştırır ve sayfalama uygulanmadan toplam yayımlanmış ürün adetlerini döndürür. Bir facet cevabındaki tüm seçenekler aynı istekte gelir; seçenek başına ürün listeleme isteği yapılmaz.

## Query parametreleri

Tüm parametreler opsiyonel ve nullable GUID değerleridir. `00000000-0000-0000-0000-000000000000` geçersizdir ve `400 validation_error` üretir.

| Parametre | Davranış |
| --- | --- |
| `TypeId` | Ürün türü filtresi. `product-types` endpointinde öz-dışlama nedeniyle sayımdan çıkarılır. |
| `BrandId` | Marka filtresi. `brands` endpointinde öz-dışlama nedeniyle sayımdan çıkarılır. |
| `CollectionId` | Koleksiyon filtresi. `collections` endpointinde öz-dışlama nedeniyle sayımdan çıkarılır. |
| `TagId` | Etiket filtresi. Üç endpointte de uygulanır. |

Diğer boyutlardaki seçili filtreler `AND` mantığıyla uygulanır. Örneğin `brands` isteğinde `TypeId` ve `CollectionId` birlikte verilirse marka adetleri yalnız bu tür ve koleksiyondaki ürünlerden hesaplanır; aynı istekteki `BrandId` ise marka seçeneklerinin kaybolmaması için dikkate alınmaz.

## Görünürlük ve sayma semantiği

Backend katalog görünürlüğünün tek otoritesidir. Sayıma yalnız aşağıdaki koşulların tamamını sağlayan ürünler girer:

- Ürün soft-delete edilmemiştir (`DeletedAtUtc == null`).
- Ürün aktiftir (`IsActive == true`).
- Ürün yayın durumundadır (`Status == Active`).

Döndürülen marka, koleksiyon veya ürün türünün kendisi de aktif olmalıdır. Eşleşen yayımlanmış ürün adedi sıfır olan sınıflandırmalar cevapta yer almaz. Koleksiyon sayıları benzersiz ürün-koleksiyon ilişkileri üzerinden hesaplanır. Sonuçlar ad, ardından kimlik ile kararlı biçimde sıralanır.

## Response

Başarılı istek `200 OK` ve bir JSON dizisi döndürür. `id`, `name` ve `productCount` zorunlu ve null olamaz.

```json
[
  {
    "id": "11111111-1111-1111-1111-111111111111",
    "name": "Marka",
    "productCount": 12
  }
]
```

## Cache ve invalidation

Endpointler `public-products` output-cache politikasını kullanır:

- TTL 30 saniyedir.
- Cache anahtarı tüm query parametrelerine göre değişir.
- Kayıtlar ortak `products` etiketiyle işaretlenir.
- Ürün oluşturma, silme, yayın/aktivasyon değişikliği ve ürün sınıflandırma ilişkisi değişiklikleri etiketi hemen geçersiz kılar.
- Marka, koleksiyon, ürün türü ve etiket oluşturma, güncelleme, silme veya aktivasyon mutasyonları da aynı etiketi geçersiz kılar.

Bu nedenle facet cevapları ürün yayınlama, pasife alma ve sınıflandırma değişikliklerinden sonra eski TTL süresini beklemeden yenilenir.
