# Sepet, Checkout ve Sipariş

Sepet hem oturum açmış müşteri hem de HttpOnly guest cart cookie ile kullanılabilir. JWT varsa kullanıcı sepeti önceliklidir.

## Sepet akışı

1. `GET /api/cart` ile authoritative sepeti okuyun.
2. Ürünü `POST /api/cart/items` ile ekleyin.
3. Her başarılı mutasyondan dönen yeni `concurrencyToken` değerini saklayın.
4. Checkout öncesi `isAvailable`, `priceChanged` ve toplamları son response üzerinden gösterin.
5. Kuponu checkout submitinden önce `POST /api/cart/coupon-preview` ile doğrulayın.

### Sepete ürün ekleme

```json
{
  "productVariantId": "f16b53be-2ddd-47f9-b247-a8605c67f65b",
  "quantity": 2,
  "expectedConcurrencyToken": "0d1ce0cb-dcf0-4b2d-819b-6fe104aec147"
}
```

İstemci Product ID, fiyat, vergi, indirim veya toplam göndermez.

### Kupon önizleme

```http
POST /api/cart/coupon-preview
Content-Type: application/json
```

```json
{
  "couponCode": "YAZ20"
}
```

Bu endpoint sepeti değiştirmez; kuponun uygulanabilirliğini ve authoritative indirim özetini döndürür.

## Üye checkout

Yetki: **User**.

```http
POST /api/orders
Authorization: Bearer <access-token>
```

```json
{
  "expectedCartConcurrencyToken": "0d1ce0cb-dcf0-4b2d-819b-6fe104aec147",
  "shippingAddressId": "0924ad10-ec24-4e2e-9067-46e92146df47",
  "shippingMethodId": "3bb1cb68-27dc-45a1-9a73-78b1356dbfb2",
  "couponCode": "YAZ20"
}
```

Başarı `201 Created` ve `OrderDto` döndürür. Sipariş oluşturulurken stok belirli süre için rezerve edilir; ödeme kesinleştiğinde rezervasyon kesinleşir.

## Misafir checkout

Yetki: **Guest cart + trusted Origin**.

Ek olarak zorunlu `Idempotency-Key`, gerektiğinde `X-Turnstile-Token` ve guest cookie kullanılır.

```json
{
  "expectedCartConcurrencyToken": "0d1ce0cb-dcf0-4b2d-819b-6fe104aec147",
  "customer": {
    "firstName": "Deniz",
    "lastName": "Yılmaz",
    "email": "deniz@example.com",
    "phoneNumber": "+905551112233"
  },
  "shippingAddress": {
    "title": "Ev",
    "firstName": "Deniz",
    "lastName": "Yılmaz",
    "phoneNumber": "+905551112233",
    "city": "İstanbul",
    "district": "Kadıköy",
    "neighborhood": "Caferağa",
    "fullAddress": "Örnek Sokak No: 1",
    "postalCode": "34710"
  },
  "billingAddress": null,
  "shippingMethodId": "3bb1cb68-27dc-45a1-9a73-78b1356dbfb2",
  "couponCode": null
}
```

Guest sipariş erişimi yedi günlük session veya kısa ömürlü tek kullanımlık bağlantı değişimiyle sağlanır. Token veya cookie değerini DOM'a/loga yazmayın.

## Sipariş sahipliği

- Üye: `GET /api/orders/mine`, `GET /api/orders/{id}`
- Guest: `GET /api/guest-orders`, `GET /api/guest-orders/{id}`
- Admin: `GET /api/orders`, `GET /api/orders/admin/{id}`

Sahibi olunmayan sipariş güvenli `404` ile gizlenir.

## Concurrency çatışması

Checkout `409 concurrency_conflict` döndürürse siparişi oluşturulmuş kabul etmeyin. Sepeti yeniden okuyun, fiyat/stok değişikliklerini kullanıcıya gösterin ve yeni checkout intent'i oluşturun.

## Ayrıntılı referans

- [Sepet endpointleri](../03-endpoint-referansi/03-satis-ve-siparis/sepet/README.md)
- [Sipariş ve ödeme endpointleri](../03-endpoint-referansi/03-satis-ve-siparis/README.md)

