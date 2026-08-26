# CartItemDto ana görsel sözleşmesi

Frontend, sepet satırında ürün görselini göstermek için ürün detayına ek istek yapmaz. `GET /api/cart` ve `CartDto` döndüren bütün mutation endpointleri her satırda nullable `mainImage` alanını taşır.

## Alan yapısı

`mainImage`, `ProductImageDto | null` tipindedir:

| Alan | TypeScript karşılığı | Nullable | Açıklama |
| --- | --- | --- | --- |
| `id` | `string` | Hayır | Görsel UUID kimliği. |
| `productId` | `string` | Hayır | `P` önekli public ürün kimliği. |
| `imageUrl` | `string` | Hayır | Sepet kartında kullanılacak görsel URL'si. |
| `altText` | `string \| null` | Evet | Görselin erişilebilir alternatif metni. |
| `displayOrder` | `number` | Hayır | Katalog görsel sırası. |
| `isMain` | `boolean` | Hayır | Görselin açıkça ana görsel seçilip seçilmediği. |

```json
{
  "productId": "P00001",
  "productTitle": "Ürün",
  "mainImage": {
    "id": "00000000-0000-0000-0000-000000000001",
    "productId": "P00001",
    "imageUrl": "https://cdn.example.com/products/main.jpg",
    "altText": "Ürün ana görseli",
    "displayOrder": 0,
    "isMain": true
  }
}
```

## Seçim ve null davranışı

API görseli deterministik olarak `isMain` azalan, `displayOrder` artan ve `id` artan sırayla seçer. Açıkça ana görsel işaretlenmemişse ilk sıralı ürün görseli fallback olur. Ürünün hiç görseli yoksa `mainImage: null` döner; frontend bu durumda kendi placeholder görselini kullanabilir.

Ana görsel, mevcut `AsSplitQuery` cart graph'ında ürün görselleriyle birlikte topluca yüklenir. Sepetteki ürün sayısına göre artan ürün detayı veya görsel isteği üretilmemelidir.

Bu sözleşme `GET /api/cart`, `POST /api/cart/items`, `PUT /api/cart/items/{cartItemId}`, `DELETE /api/cart/items/{cartItemId}`, `DELETE /api/cart`, `POST /api/cart/merge-guest` ve claim cevabındaki `cart.items` için aynıdır. Generated frontend tipleri `openapi-controller-contract.json` üzerinden yeniden üretilmeli; elle değiştirilmemelidir.
