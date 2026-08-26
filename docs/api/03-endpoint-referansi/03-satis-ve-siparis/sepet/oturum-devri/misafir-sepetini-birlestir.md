# POST /api/cart/merge-guest

- Görev alanı: **Satış ve sipariş → Sepet → Sepet oturum devri**.
- İşlev: Misafir sepetini birleştirir.
- Operation ID: `POST-/api/cart/merge-guest`
- Yetki: **User**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "concurrencyToken": "00000000-0000-0000-0000-000000000001",
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000001",
      "productId": "string",
      "productVariantId": "00000000-0000-0000-0000-000000000001",
      "productTitle": "string",
      "mainImage": {
        "id": "00000000-0000-0000-0000-000000000001",
        "productId": "P00001",
        "imageUrl": "https://cdn.example.com/products/main.jpg",
        "altText": "Ürün ana görseli",
        "displayOrder": 0,
        "isMain": true
      },
      "variantName": "string",
      "variantValue": "string",
      "sku": "string",
      "quantity": 1,
      "unitPrice": 1,
      "currentUnitPrice": 1,
      "totalPrice": 1,
      "availableStock": 1,
      "isAvailable": true,
      "priceChanged": true,
      "createdAt": "2026-07-29T12:00:00Z"
    }
  ],
  "totalQuantity": 1,
  "subTotal": 1,
  "hasUnavailableItems": true,
  "hasPriceChanges": true,
  "createdAt": "2026-07-29T12:00:00Z",
  "updatedAt": "2026-07-29T12:00:00Z"
}
```

`variantName` ve `variantValue` nullable ve en fazla 150 karakterdir. Varyantsız üründe ikisi de `null` döner. Ayrıntı: [varyant snapshot sözleşmesi](../varyant-snapshot-sozlesmesi.md).

`mainImage` nullable `ProductImageDto` değeridir; ürün görseli yoksa `null` döner. Ayrıntı: [CartItemDto ana görsel sözleşmesi](../kalem-ana-gorsel-sozlesmesi.md).

Bu endpoint geriye dönük uyumluluk içindir. Artık yalnız sepeti değil, aynı `ecommerce_guest_cart` session'ına ait sepet ve favorileri tek serializable transaction içinde claim eder; response yine yalnız `CartDto` döndürür. Yeni istemciler iki son durumu da almak için `POST /api/guest-session/claim` kullanmalıdır.

- Üye sepeti yok veya boşsa guest sepet içeriği güncel aktiflik, fiyat ve stok kontrolünden sonra benimsenir.
- Üye sepeti doluysa üye sepeti aynen korunur; guest sepet silinir ve ürünler birleştirilmez.
- Üyenin favorisi yoksa guest favoriler owner değişikliğiyle devredilir; ürün sayaçları tekrar artırılmaz.
- Üyenin en az bir favorisi varsa üye favorileri aynen korunur; guest favoriler kaldırılır ve güncel ürün sayaçları düzeltilir.
- İşlem başarısızsa hiçbir alan kısmen claim edilmez ve cookie korunur. Başarılı response sonrasında API hem `/api` hem eski `/api/cart` path cookie'sini siler.

Başarı/hata kodları: `200`, `400`, `401`, `409`.

