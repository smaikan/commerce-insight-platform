# Sepet API’leri

## Sahiplik ve cookie

Cart endpointleri anonymous çalışır. JWT varsa User cart her zaman guest cookie’den önceliklidir. JWT yoksa API 256 bit rastgele `ecommerce_guest_cart` cookie’si üretir: Secure, HttpOnly, SameSite=Lax, `/api` path, 30 gün. Bu ortak session hem guest cart hem guest favorites sahibi olur. Frontend cookie değerini client JavaScript ile okuyamaz veya üretmez; Next.js BFF upstream `Set-Cookie` değerini storefront origin’e güvenli seçeneklerle aktarır.

| Method | Endpoint | Açıklama |
| --- | --- | --- |
| GET | `/api/cart` | Güncel owner cart |
| POST | `/api/cart/items` | Varyant/adet ekle |
| PUT | `/api/cart/items/{cartItemId}` | Adet güncelle |
| DELETE | `/api/cart/items/{cartItemId}?expectedConcurrencyToken=...` | Kalem sil |
| DELETE | `/api/cart?expectedConcurrencyToken=...` | Cart temizle |
| POST | `/api/guest-session/claim` | JWT sonrası guest cart ve favorileri tek işlemde claim et |
| POST | `/api/cart/merge-guest` | Geriye uyumlu claim; response yalnız `CartDto` |
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
    "productTitle": "Ürün",
    "mainImage": {
      "id": "54b...",
      "productId": "P00001",
      "imageUrl": "https://cdn.example.com/products/main.jpg",
      "altText": "Ürün ana görseli",
      "displayOrder": 0,
      "isMain": true
    },
    "variantName": "Renk",
    "variantValue": "Pudra",
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

`mainImage`, backend'in ana görsel önceliğiyle seçtiği nullable `ProductImageDto` değeridir. Ürünün görseli yoksa `null` döner; frontend ürün başına ek katalog isteği yapmamalıdır. Ayrıntılı sözleşme: [CartItemDto ana görsel sözleşmesi](../08-endpoint-sozlesmeleri/04-sepet/CART-ITEM-MAIN-IMAGE-SOZLESMESI.md).

Frontend request’te yalnız `productVariantId`, `quantity` ve concurrency token gönderebilir. Product ID, fiyat, vergi, stok ve toplam gönderemez.

`variantName` ve `variantValue`, varyantlı üründe güncel seçimi ayrı alanlarda taşır. Varyantsız ürünlerde iki alan da `null` olur ve teknik `Default/Varsayılan` değeri müşteriye sızmaz. Ayrıntılı sözleşme: [Sepet ve sipariş varyant snapshot sözleşmesi](../08-endpoint-sozlesmeleri/04-sepet/SEPET-SIPARIS-VARYANT-SNAPSHOT-SOZLESMESI.md).

## Login claim ve checkout temizliği

Login sonrasında tercih edilen akış `POST /api/guest-session/claim` çağrısıdır. Mevcut `POST /api/cart/merge-guest` geriye uyumludur ve aynı atomik cart+favorites claim servisini çalıştırır.

- Üye sepeti yok veya boşsa guest sepet içeriği güncel aktiflik, fiyat ve stok doğrulamasıyla benimsenir.
- Üye sepeti doluysa üye sepeti aynen korunur; guest satırlar birleştirilmez ve guest sepet kaldırılır.
- Üyenin favorisi yoksa guest favoriler sayaçları yeniden artırmadan devredilir.
- Üyenin herhangi bir favorisi varsa üye listesi aynen korunur; guest favoriler birleştirilmez ve özet sayaçları düzeltilerek kaldırılır.

Cart ile favorites tek serializable transaction içindedir. Başarısız işlem hiçbir kısmı kalıcılaştırmaz ve cookie retry için korunur. Başarılı response sonrasında `/api` ve eski `/api/cart` cookie path kayıtları silinir. Başarılı üye veya guest checkout cart kalemlerini aynı transaction’da temizler.

## Idempotency ve Next.js BFF

Aynı checkout butonu/double-submit/network retry tek intent’tir ve aynı `Idempotency-Key` kullanır. Kullanıcı cart/adres/kargo/kuponu değiştirirse yeni intent/key üretir. BFF:

- browser’dan same-origin isteği kabul eder ve `Origin` doğrular;
- yalnız guest cart/order/CSRF cookie allowlist’ini upstream’e taşır;
- `Idempotency-Key` ve gerekirse Turnstile header’ını korur;
- upstream `Set-Cookie` değerlerini storefront origin için yeniden yazar;
- cookie değerini Client Component, DOM, localStorage, log ve analytics’e açmaz;
- cart/checkout cevaplarında `Cache-Control: no-store` uygular.
