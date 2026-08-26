# Katalog ve Ürünler

Katalog API'si iki farklı kullanım sınırına sahiptir:

- **Storefront okuması:** yalnız yayında ve görünür ürünleri, filtre seçeneklerini ve vitrin sınıflandırmalarını public olarak sunar.
- **Admin yönetimi:** ürün, varyant, görsel, marka, ürün tipi, koleksiyon, etiket ve banner verisini yönetir.

## Storefront ürün listesi

```http
GET /api/products/published?PageNumber=1&PageSize=24&Search=kolye&SortBy=0&Descending=true
```

Yetki: **Public**.

Yanıt sayfalıdır ve kart için gereken ürün özeti, ana görsel, fiyat ve görünürlük bilgilerini taşır. Storefront aynı satır için ayrıca ürün detayı/görsel isteği yaparak N+1 oluşturmamalıdır.

Public filtre seçenekleri ürün adediyle birlikte şu endpointlerden alınır:

- `GET /api/products/published/facets/brands`
- `GET /api/products/published/facets/collections`
- `GET /api/products/published/facets/product-types`

## Ürün detayı ve SEO

- `GET /api/products/by-url/{url}` public ürün/SEO detayını döndürür.
- `GET /api/products/seo-index` sitemap benzeri tüketiciler için sayfalı URL indeksidir.
- `GET /api/products/published/search-suggestions` küçük arama önerisi payloadı döndürür ve ayrı rate limit uygular.

Public olmayan `GET /api/products/{id}` admin ürün detay endpointidir ve `P...` biçiminde Product ID ister.

## Ürün oluşturma

Yetki: **Admin**.

```http
POST /api/products
Authorization: Bearer <admin-access-token>
Content-Type: application/json
```

```json
{
  "title": "Kırmızı Keten Gömlek",
  "mainSku": "GOM-KET-001",
  "hasVariants": true,
  "type": "Gömlek",
  "url": "kirmizi-keten-gomlek",
  "brandId": "099a9a53-d1d9-49e3-b5db-714e7b836995",
  "description": "Keten karışımlı kadın gömlek.",
  "status": 1,
  "isFeatured": false,
  "displayOrder": 10,
  "collections": [
    "Yaz Koleksiyonu"
  ],
  "tags": [
    "keten",
    "yeni"
  ],
  "taxRateId": "0f5125de-7d4f-4e3e-bfd3-b5b1f3ef7bd1",
  "variants": [
    {
      "name": "Renk / Beden",
      "value": "Kırmızı / M",
      "sku": "GOM-KET-KRM-M",
      "price": 1299.9,
      "stock": 8,
      "isActive": true,
      "openingUnitCostExcludingVat": 620,
      "openingUnitCostIncludingVat": 682
    }
  ]
}
```

Birden fazla varyant varsa `hasVariants=true` olmalıdır. Varyantın `name` ve `value` alanları sıralı eksenlerdir; örnekte `Renk / Beden` ile `Kırmızı / M` aynı sırada eşleşir.

## Varyant ve stok ayrımı

- Varyant temel bilgisi `POST/PUT /api/product-variants...` ile yönetilir.
- Normal operasyonel stok, ürün/varyant update body’sinde doğrudan set edilmez.
- Stok değişimi imzalı `StockMovement` endpointleriyle yapılır.
- Toplu varyant güncellemesi satır bazlı `expectedConcurrencyToken` ister ve SKU takaslarını tek transaction içinde uygular.

## Sınıflandırmalar

| Kaynak | Kullanım |
| --- | --- |
| ProductType | Ürünün ana kategorisi |
| Collection | Merchandising/vitrin grubu; bir ürün birden fazla koleksiyonda olabilir |
| Tag | Arama, filtreleme veya editoryal etiket |
| Brand | Ürün markası |

`GET /api/collections/published` yalnız aktif ve en az bir görünür ürünü olan koleksiyonları döndürür. ProductType vitrini özel görsel yoksa görünür ürünlerden popülerlik temelli fallback görsel seçebilir.

## Ayrıntılı referans

- [Katalog ve etkileşim endpointleri](../03-endpoint-referansi/02-katalog/README.md)
- [Stok endpointleri](../03-endpoint-referansi/04-operasyon/README.md)

