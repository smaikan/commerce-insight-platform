# POST /api/orders

JWT kullanıcısının sepetini siparişe dönüştüren üye checkout ucudur. Guest için bu uç değil `POST /api/cart/checkout/guest` kullanılır.

## İstek sözleşmesi

- Authorization: `Bearer <JWT>` zorunlu.
- Üyenin kayıtlı shipping adresi ve aktif shipping yöntemi zorunludur.
- Fiyat, vergi, indirim, kargo ücreti, stok, toplam ve `userId` frontend'den alınmaz.

| Body | Required | Nullable | Kural |
| --- | --- | --- | --- |
| expectedCartConcurrencyToken | Evet | Hayır | Son GET/mutasyon cevabındaki token |
| shippingAddressId | Evet | Hayır | JWT kullanıcısının aktif adresi |
| shippingMethodId | Evet | Hayır | Aktif kargo yöntemi |
| couponCode | Hayır | Evet | Uygun kupon; üye `isMemberOnly` kupon kullanabilir |

```http
POST /api/orders
Authorization: Bearer <jwt>
Content-Type: application/json

{"expectedCartConcurrencyToken":"f30e9ea5-2ac2-4af4-a03c-d765e1d8cf46","shippingAddressId":"d7df4362-ccaf-4c9f-8dfd-144959e52931","shippingMethodId":"a3d88341-c03e-49f1-992c-79d68c388491","couponCode":"SAVE20"}
```

## Başarılı cevap

`200 OrderDto` döner. Müşteri, shipping/billing adresi, kargo adı/ücreti, ürün/fiyat/vergi/kupon değerleri snapshot'tır. Ayrı billing adresi bu üye sözleşmesinde alınmadığı için billing snapshot shipping snapshot'tan üretilir.

```json
{"id":"3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26","orderNumber":"ORD-20260803-001","status":1,"customer":{"firstName":"Ayşe","lastName":"Yılmaz","email":"ayse@example.com","phoneNumber":"+905551112233"},"shippingAddress":{"sourceAddressId":"d7df4362-ccaf-4c9f-8dfd-144959e52931","city":"İstanbul"},"billingAddress":{"sourceAddressId":"d7df4362-ccaf-4c9f-8dfd-144959e52931","city":"İstanbul"},"shippingMethodName":"Standart Kargo","grandTotal":1299.90,"reservationExpiresAt":"2026-08-03T12:15:00Z","items":[{"variantSku":"SKU-PUDRA","variantName":"Renk","variantValue":"Pudra"}],"payments":[]}
```

`OrderItemDto.variantName` ve `variantValue` checkout anındaki değişmez snapshot'lardır; varyantsız veya eski siparişte `null` olabilir. Ayrıntı: [varyant snapshot sözleşmesi](../04-sepet/SEPET-SIPARIS-VARYANT-SNAPSHOT-SOZLESMESI.md).

Transaction içinde Order/snapshot/kalem/kupon kullanımı, negatif `Sale` StockMovement, 15 dakikalık rezervasyon, metrik, cart temizliği ve outbox kayıtları oluşur. Outbox e-postasının SMTP gönderimini beklemez. Sıfır toplamlı kupon siparişinde Order oluşur; payment ucu çağrılmaz.

## Hatalar ve frontend recovery

- `400 validation_error`: zorunlu adres/kargo/token alanı eksik.
- `401/403`: üyelik/yetki sorunu.
- `404`: adres, shipping veya sepet kaynağı bulunamadı.
- `409`: stale cart, kupon/stok/kargo checkout sırasında geçersiz. Stale cart'ta GET cart yapıp son token ve fiyat değişikliklerini göster.
- `500`: sonucu sipariş listesinde doğrulamadan yeni checkout yapma.

Cart concurrency token eşzamanlı sepet değişikliğini, ödeme/guest checkout idempotency key ise tekrarlanan intent'i çözer; aynı kavram değildir. JWT, adres PII'si ve sipariş cevapları log/analytics'e yazılmaz.
