# GET /api/product-engagement/favorites

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Kaynağı veya filtrelenmiş kaynak listesini okur.
- Operation ID: `GET-/api/product-engagement/favorites`
- Yetki: `AllowAnonymous`; JWT varsa yalnız kullanıcı favorileri, JWT yoksa ortak guest session favorileri döner.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `pageNumber` | query | Hayır | integer (int32) |
| `pageSize` | query | Hayır | integer (int32) |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

Response `ProductDtoPagedResult` tipindedir:

```json
{
  "items": [
    {
      "id": "P00001",
      "title": "Ürün",
      "variants": [],
      "tags": [],
      "collections": [],
      "images": [],
      "mainImage": null
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

Her öğe tam `ProductDto` sözleşmesini kullanır. API; type, brand, tax rate, variants, images, collections ve tags ilişkilerini toplu olarak yükler. Sonuç başlık ve ardından ürün kimliğiyle kararlı sıralanır; N+1 sorgu üretilmez. Favorisi olmayan owner `200` ve boş `items` alır.

Anonim ilk istekte API, cart ile ortak kullanılan `ecommerce_guest_cart` cookie'sini üretir veya yeniler. Cookie 256 bit uppercase hexadecimal token taşır; `Secure`, `HttpOnly`, `SameSite=Lax`, `Path=/api` ve 30 gün ömürlüdür. Geçerli JWT her zaman cookie'den önceliklidir. `Authorization` header gönderilmiş fakat token doğrulanamamışsa guest'e düşülmez, `401` döner.

Başarı/hata kodları: `200`, `400`, `401`.

