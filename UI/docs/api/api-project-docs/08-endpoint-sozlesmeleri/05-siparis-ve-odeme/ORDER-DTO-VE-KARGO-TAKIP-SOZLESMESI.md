# OrderDto ürün medya ve kargo takip sözleşmesi

Bu sözleşme üye, guest ve admin sipariş detaylarında dönen ortak `OrderDto` modelini tamamlar.

## OrderItemDto snapshot alanları

| Alan | Tip | Nullable | Maksimum | Semantik |
| --- | --- | --- | --- | --- |
| `productUrl` | string | Evet | 250 | Checkout anındaki ürün slug değeri. Mutlak URL değildir. |
| `imageUrl` | string | Evet | 500 | Checkout anında `isMain`, `displayOrder`, `id` önceliğiyle seçilen görsel URL'si. |
| `imageAlt` | string | Evet | 250 | Seçilen görselin checkout anındaki alt metni. |
| `variantName` | string | Evet | 150 | Checkout anındaki seçenek adı snapshot'ı; örneğin `Renk`. |
| `variantValue` | string | Evet | 150 | Checkout anındaki seçenek değeri snapshot'ı; örneğin `Pudra`. |

Yeni checkout'larda bu alanlar canlı ürün tablosundan okunup `OrderItem` üzerinde saklanır. Ürün slug'ı, ana görseli veya alt metni sonradan değişse bile geçmiş sipariş cevabı değişmez. Görsel bulunmayan ürünlerde `imageUrl` ve `imageAlt` `null` olur. Migration öncesi veya dışarıdan aktarılmış kayıtlarda `productUrl` da `null` olabilir.

Varyant adı ve değeri de canlı katalogdan yeniden okunmaz. Varyantsız ürünlerde teknik tek-varyant metni yerine `variantName=null` ve `variantValue=null` döner. Tam kullanım kuralı [varyant snapshot sözleşmesinde](../04-sepet/SEPET-SIPARIS-VARYANT-SNAPSHOT-SOZLESMESI.md) açıklanır.

Frontend güvenli ürün bağlantısını aşağıdaki gibi kurmalıdır:

```ts
const href = item.productUrl
  ? `/products/${encodeURIComponent(item.productUrl)}`
  : null;
```

`productId` kullanılarak URL tahmin edilmemeli, `productUrl` mutlak origin gibi yorumlanmamalıdır.

## OrderDto kargo takip alanları

| Alan | Tip | Nullable | Maksimum | Semantik |
| --- | --- | --- | --- | --- |
| `shippingCarrier` | string | Evet | 150 | Gerçek taşıyıcı/kargo firması snapshot'ı; checkout kargo yöntemi değildir. |
| `trackingNumber` | string | Evet | 100 | Taşıyıcının takip numarası. |
| `trackingUrl` | string | Evet | 500 | Mutlak HTTP/HTTPS takip bağlantısı. |
| `shippedAt` | string/date-time | Evet | - | API'nin siparişi ilk kez `Shipped` yaptığı UTC an. |
| `deliveredAt` | string/date-time | Evet | - | API'nin siparişi `Delivered` yaptığı UTC an. |

`shippingMethodName`, checkout'ta seçilen ücretlendirme yönteminin snapshot'ıdır; `shippingCarrier` ile aynı kavram değildir.

## Admin kargoya verme isteği

`PATCH /api/orders/{id}/status` AdminOnly endpointidir. `status=Shipped` geçişinde taşıyıcı ve takip numarası zorunludur:

```json
{
  "status": 4,
  "shippingCarrier": "Yurtiçi Kargo",
  "trackingNumber": "1234567890",
  "trackingUrl": "https://www.example-cargo.test/track/1234567890"
}
```

- `trackingUrl` opsiyoneldir; `javascript:`, `ftp:`, protocol-relative ve relative adresler 400 döndürür.
- `shippedAt` ve `deliveredAt` request alanı değildir; sunucu üretir.
- Sipariş `Preparing` durumundaysa istek kargo bilgisini atomik saklar ve durumu `Shipped` yapar.
- Sipariş zaten `Shipped` ise aynı sözleşme takip bilgisini düzeltir, ilk `shippedAt` değerini değiştirmez.
- `Delivered` geçişi mevcut status endpointiyle yapılır ve `deliveredAt` API tarafından atanır.
- Geçersiz yaşam döngüsü 400/409 ortak `ProblemDetails` sözleşmesiyle döner.

Bu alanlar aşağıdaki tüm `OrderDto` cevaplarında aynıdır:

- `POST /api/orders`
- `GET /api/orders/{id}`
- `GET /api/orders/admin/{id}`
- `GET /api/guest-orders/{id}`
- üye/guest ödeme, iptal ve admin durum mutation cevapları
