# GET /api/guest-orders/{id}

Guest session'ın grant aldığı tek siparişin kalem, müşteri, shipping/billing, kargo ve ödeme snapshot'larını döndürür. Ürün medya snapshot'ları ve kargo takip geçmişi için [ortak OrderDto sözleşmesine](ORDER-DTO-VE-KARGO-TAKIP-SOZLESMESI.md), varyant adı/değeri için [varyant snapshot sözleşmesine](../04-sepet/SEPET-SIPARIS-VARYANT-SNAPSHOT-SOZLESMESI.md) bakın.

## İstek sözleşmesi

- Authorization: JWT gerekmez.
- Cookie: `ecommerce_guest_orders` zorunlu.
- Cevap: `no-store`.

| Route | Required | Nullable | Kural |
| --- | --- | --- | --- |
| id | Evet | Hayır | Order GUID |

```http
GET /api/guest-orders/3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26
Cookie: ecommerce_guest_orders=<session>
```

## Başarılı cevap

```json
{"id":"3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26","userId":null,"orderNumber":"ORD-20260803-001","status":1,"customer":{"firstName":"Ayşe","lastName":"Yılmaz","email":"ayse@example.com","phoneNumber":"+905551112233"},"shippingAddress":{"sourceAddressId":null,"type":1,"city":"İstanbul"},"billingAddress":{"sourceAddressId":null,"type":2,"city":"İstanbul"},"shippingMethodName":"Standart Kargo","shippingFee":49.90,"grandTotal":1299.90,"items":[],"payments":[]}
```

Cookie değişmez. Snapshot alanları sipariş tarihindeki değerdir; güncel profil/kargo kaydıyla değiştirilmez.

## Hatalar, retry ve UI davranışı

- `400`: GUID biçimi geçersiz.
- `401 invalid_guest_access`: session yenilemek için access-link iste.
- `404 not_found`: sipariş yok **veya session bu siparişe yetkili değil**; ayrım yapılmaz.
- `403/409/428/429/503`: normal GET akışında beklenmez.
- `500`: PII göstermeyen genel hata ve kontrollü retry.

Loading skeleton kullanılabilir; yanlış sipariş numarası/e-posta asla bu uçta yetki değildir. Cookie ve PII loglanmaz.
