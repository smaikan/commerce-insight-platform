# GET /api/product-engagement/products/{productId}/reviews

- Görev alanı: **Katalog → Ürün etkileşimleri → Yorumlar**.
- İşlev: Ürün yorumlarını listeler.
- Operation ID: `GET-/api/product-engagement/products/{productId}/reviews`
- Yetki: **Public**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `productId` | path | Evet | string |
| `pageNumber` | query | Hayır | integer (int32) |
| `pageSize` | query | Hayır | integer (int32) |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

Response body yoktur.

