# PUT /api/cart/items/{cartItemId}

- Görev alanı: **Satış ve sipariş → Sepet → Sepet kalemleri**.
- İşlev: Sepet kalemini günceller.
- Operation ID: `PUT-/api/cart/items/{cartItemId}`
- Yetki: **Public / guest cart**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `cartItemId` | path | Evet | string (uuid) |

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `quantity` | integer (int32) | Evet |
| `expectedConcurrencyToken` | string (uuid) | Evet |

```json
{
  "quantity": 1,
  "expectedConcurrencyToken": "00000000-0000-0000-0000-000000000001"
}
```

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

