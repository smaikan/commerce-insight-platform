# Sipariş ürün medya ve kargo takip sözleşmesi

Sipariş kalemleri checkout anındaki ürün bağlantısı ve ana görseli snapshot olarak saklar. Bu alanlar ürün daha sonra değiştirilse veya görsel sırası güncellense bile geçmiş siparişte değişmez.

`OrderItemDto` alanları:

- `productUrl`: checkout anındaki ürün slug değeri; eski veya dışarıdan aktarılmış siparişlerde `null` olabilir.
- `imageUrl`: checkout anında ana kabul edilen görselin URL'si; görsel yoksa `null`.
- `imageAlt`: aynı görselin alt metni; alt metin yoksa `null`.

Frontend ürün bağlantısını `/products/${encodeURIComponent(productUrl)}` biçiminde kurmalıdır. `productUrl` mutlak origin değildir.

`OrderDto` ayrıca `shippingCarrier`, `trackingNumber`, `trackingUrl`, `shippedAt` ve `deliveredAt` alanlarını döndürür. Kargoya verilmemiş veya eski kayıtlar için bu alanlar `null` olabilir.

Yönetici `PATCH /api/orders/{id}/status` isteğinde `status=Shipped` gönderirken `shippingCarrier` ve `trackingNumber` alanlarını zorunlu gönderir. `trackingUrl` isteğe bağlıdır; doluysa en fazla 500 karakterli mutlak HTTP/HTTPS URL olmalıdır. `shippedAt` ve `deliveredAt` istemciden alınmaz, API tarafından UTC olarak üretilir. `Shipped` durumundaki siparişin takip bilgisi aynı istekle düzeltilebilir ve ilk `shippedAt` korunur.
