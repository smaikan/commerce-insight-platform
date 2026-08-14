# Sepet ve sipariş varyant snapshot sözleşmesi

Frontend, seçilen varyantı göstermek için ürün detayına ek istek yapmaz. API hem sepet hem sipariş satırında seçenek adını ve değerini ayrı alanlarla verir.

## CartItemDto

| Alan | TypeScript karşılığı | Nullable | Maksimum | Kaynak |
| --- | --- | --- | --- | --- |
| `variantName` | `string \| null` | Evet | 150 | Varyantlı üründe güncel `ProductVariant.name`; örnek `Renk`. |
| `variantValue` | `string \| null` | Evet | 150 | Varyantlı üründe güncel `ProductVariant.value`; örnek `Pudra`. |

`GET /api/cart` ve `CartDto` döndüren bütün mutation endpointleri aynı `CartItemDto` sözleşmesini kullanır. `Renk · Pudra` seçimi aşağıdaki gibi gelir:

```json
{
  "productVariantId": "00000000-0000-0000-0000-000000000001",
  "variantName": "Renk",
  "variantValue": "Pudra",
  "sku": "SKU-PUDRA"
}
```

`Product.hasVariants=false` olan ürünlerde teknik tek varyant veritabanında bulunabilir; buna rağmen `variantName` ve `variantValue` public cevapta `null` döner. Frontend `Default`, `Varsayılan` veya SKU fallback'i göstermemelidir.

## OrderItemDto

| Alan | TypeScript karşılığı | Nullable | Maksimum | Kaynak |
| --- | --- | --- | --- | --- |
| `variantName` | `string \| null` | Evet | 150 | Checkout anındaki değişmez seçenek adı snapshot'ı. |
| `variantValue` | `string \| null` | Evet | 150 | Checkout anındaki değişmez seçenek değeri snapshot'ı. |
| `variantSku` | `string` | Hayır | 100 | Checkout anındaki değişmez SKU snapshot'ı. |

Üye ve guest checkout bu alanları aynı backend akışında snapshot'lar. Canlı varyantın adı/değeri sonradan değişse bile geçmiş sipariş değişmez. Migration öncesi siparişlerde ve varyantsız ürünlerde iki yeni alan `null` olabilir.

Bu alanlar `POST /api/orders`, `POST /api/cart/checkout/guest`, `GET /api/orders/{id}`, `GET /api/orders/admin/{id}`, `GET /api/guest-orders/{id}` ve `OrderDto` döndüren diğer mutation cevaplarında aynıdır.

## Render önerisi

```ts
const variantLabel = item.variantName && item.variantValue
  ? `${item.variantName}: ${item.variantValue}`
  : null;
```

Ayraç ve görsel biçim frontend'e aittir; API birleşik/yerelleştirilmiş `variantDisplayName` üretmez. Generated tipler `openapi-controller-contract.json` üzerinden yeniden üretilmeli, elle düzenlenmemelidir.
