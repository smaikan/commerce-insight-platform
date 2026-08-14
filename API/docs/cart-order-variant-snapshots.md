# Sepet ve sipariş varyant snapshot sözleşmesi

`CartItemDto` ve `OrderItemDto`, seçilen varyantın adını ve değerini ayrı nullable alanlarla taşır. Frontend bu bilgi için ürün detay endpointine ek istek yapmamalıdır.

## CartItemDto

| Alan | Tip | Nullable | Maksimum | Semantik |
| --- | --- | --- | --- | --- |
| `variantName` | string | Evet | 150 | Varyantlı üründeki seçenek adı; örneğin `Renk`. |
| `variantValue` | string | Evet | 150 | Seçilen seçenek değeri; örneğin `Pudra`. |

Ürünün `hasVariants` değeri `true` ise iki alan güncel katalogdaki `ProductVariant.Name` ve `ProductVariant.Value` değerlerinden üretilir. `hasVariants=false` olan teknik tek-varyantlı ürünlerde iki alan da `null` döner; `Default` veya `Varsayılan` gibi dahili metinler public cevaba taşınmaz.

Bu sözleşme `GET /api/cart` ile `POST /api/cart/items`, `PUT /api/cart/items/{cartItemId}`, `DELETE /api/cart/items/{cartItemId}`, `DELETE /api/cart` ve `POST /api/cart/merge-guest` cevaplarında kullanılan ortak `CartDto` için aynıdır.

## OrderItemDto

| Alan | Tip | Nullable | Maksimum | Semantik |
| --- | --- | --- | --- | --- |
| `variantName` | string | Evet | 150 | Checkout anındaki seçenek adı snapshot'ı. |
| `variantValue` | string | Evet | 150 | Checkout anındaki seçenek değeri snapshot'ı. |
| `variantSku` | string | Hayır | 100 | Checkout anındaki SKU snapshot'ı. |

Üye ve guest checkout aynı `OrderCheckoutOrchestrator` üzerinden bu değerleri `OrderItem` kaydına yazar. Ürün veya varyant daha sonra değiştirilse bile geçmiş sipariş cevabı değişmez. Varyantsız ürünlerde ve migration öncesindeki eski siparişlerde `variantName` ile `variantValue` `null` olabilir.

Alanlar `POST /api/orders`, `POST /api/cart/checkout/guest` ve `OrderDto` döndüren üye, guest ve admin sipariş detay/mutasyon cevaplarında aynıdır.

## Frontend kullanım kuralı

- İki alan doluysa kullanıcıya ad ve değeri birlikte gösterin; ayraç/görsel biçim frontend sorumluluğundadır.
- İki alan `null` ise teknik varyant adı, SKU veya `Default` fallback'i göstermeyin.
- Geçmiş siparişte canlı ürün/varyant endpointine giderek snapshot'ı yeniden üretmeyin.
- `variantName` ile `variantValue` nullable olduğundan generated client tiplerini değiştirmeden OpenAPI'den yeniden üretin.

Kalıcı alanlar `20260813092252_AddOrderItemVariantSnapshots` migration'ıyla nullable `nvarchar(150)` kolonlar olarak eklenmiştir.
