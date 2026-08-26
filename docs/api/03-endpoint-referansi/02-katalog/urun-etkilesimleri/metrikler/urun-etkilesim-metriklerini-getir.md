# GET /api/product-engagement/products/{productId}/metrics

- Görev alanı: **Katalog → Ürün etkileşimleri → Metrikler**.
- İşlev: Bir ürünün seçilen gün aralığındaki günlük etkileşim sayaçlarını okur.
- Operation ID: `GET-/api/product-engagement/products/{productId}/metrics`
- Yetki: **Admin**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `productId` | path | Evet | string |
| `from` | query | Evet | string (date, `YYYY-MM-DD`) |
| `to` | query | Evet | string (date, `YYYY-MM-DD`) |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

```json
[
  {
    "date": "2026-08-01",
    "clickCount": 42,
    "addToCartCount": 6,
    "purchaseCount": 2,
    "favoriteCount": 3,
    "ratingCount": 1,
    "reviewCount": 1
  }
]
```

Yanıt `ProductMetricDto[]` şeklindedir. Seçilen aralıktaki her UTC gün için bir kayıt döner; hareketsiz günlerin sayaçları `0` olur. `from`, `to` ve günlük metric yazımı UTC gün sınırını kullanır. `from` değeri `to` değerinden sonra olamaz ve iki uç dahil aralık en fazla 90 gün olabilir.

