# DELETE /api/product-images/{id}

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Ürün görselini fiziksel olarak siler; silinen görsel ana görselse sıradaki görseli ana görsel yapar.
- Operation ID: `DELETE-/api/product-images/{id}`
- Yetki: `AdminOnly`.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `id` | path | Evet | string (uuid) |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (204)

Response body yoktur.

