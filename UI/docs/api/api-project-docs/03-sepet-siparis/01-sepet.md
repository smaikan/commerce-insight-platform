# Sepet API’leri

## Sahiplik ve cookie

Cart endpointleri anonymous çalışır. JWT varsa User cart her zaman guest cookie’den önceliklidir. JWT yoksa API 256 bit rastgele `ecommerce_guest_cart` cookie’si üretir: Secure, HttpOnly, SameSite=Lax, `/api/cart` path, 30 gün. Frontend cookie değerini okuyamaz veya üretmez; Next.js BFF upstream `Set-Cookie` değerini storefront origin’e güvenli seçeneklerle aktarır.

| Method | Endpoint | Açıklama |
| --- | --- | --- |
| GET | `/api/cart` | Güncel owner cart |
| POST | `/api/cart/items` | Varyant/adet ekle |
| PUT | `/api/cart/items/{cartItemId}` | Adet güncelle |
| DELETE | `/api/cart/items/{cartItemId}?expectedConcurrencyToken=...` | Kalem sil |
| DELETE | `/api/cart?expectedConcurrencyToken=...` | Cart temizle |
| POST | `/api/cart/merge-guest` | JWT sonrası guest cart’ı üyeye bir kez birleştir |
| POST | `/api/cart/checkout/guest` | Guest checkout |

## Concurrency token yaşam döngüsü

İlk boş cart add isteğinde `expectedConcurrencyToken=null` olabilir. Cart oluştuktan sonra her başarılı mutasyon yeni `concurrencyToken` döndürür. UI response’u aldıktan sonra bellekteki tokenı hemen değiştirir; paralel tab eski token gönderirse `409 concurrency_conflict` alır. Recovery:

1. Mutasyon draft’ını koru.
2. `GET /api/cart` ile otoriter cart’ı oku.
3. Değişen ürün/adet/fiyat/uygunluk durumunu göster.
4. Kullanıcı yeniden karar verirse son tokenla yeni mutasyon gönder.

## CartDto ve katalog değişiklikleri

```json
{
  "id": "6cf...",
  "concurrencyToken": "fc3...",
  "items": [{
    "id": "02a...",
    "productId": "P00001",
    "productVariantId": "0f7...",
    "quantity": 2,
    "unitPrice": 499.90,
    "currentUnitPrice": 529.90,
    "totalPrice": 1059.80,
    "availableStock": 8,
    "isAvailable": true,
    "priceChanged": true
  }],
  "totalQuantity": 2,
  "subTotal": 1059.80,
  "hasUnavailableItems": false,
  "hasPriceChanges": true
}
```

`unitPrice` cart snapshot’ı, `currentUnitPrice` güncel katalog fiyatıdır. `priceChanged=true` ise kullanıcıya değişiklik gösterilir; checkout yine backend’de güncel fiyatla yeniden hesaplanır. `isAvailable=false` olan kalem checkout’u engeller.

Frontend request’te yalnız `productVariantId`, `quantity` ve concurrency token gönderebilir. Product ID, fiyat, vergi, stok ve toplam gönderemez.

## Login merge ve checkout temizliği

Login sonrasında `POST /api/cart/merge-guest` JWT ile çağrılır. Backend guest cart miktarlarını üye cart’ına güvenli kurallarla birleştirir; başarılı cevap sonrası guest cart cookie silinir. Başarılı üye veya guest checkout cart kalemlerini aynı transaction’da temizler. Guest cart cookie kalabilir ancak arkasındaki cart boştur.

## Idempotency ve Next.js BFF

Aynı checkout butonu/double-submit/network retry tek intent’tir ve aynı `Idempotency-Key` kullanır. Kullanıcı cart/adres/kargo/kuponu değiştirirse yeni intent/key üretir. BFF:

- browser’dan same-origin isteği kabul eder ve `Origin` doğrular;
- yalnız guest cart/order/CSRF cookie allowlist’ini upstream’e taşır;
- `Idempotency-Key` ve gerekirse Turnstile header’ını korur;
- upstream `Set-Cookie` değerlerini storefront origin için yeniden yazar;
- cookie değerini Client Component, DOM, localStorage, log ve analytics’e açmaz;
- cart/checkout cevaplarında `Cache-Control: no-store` uygular.
