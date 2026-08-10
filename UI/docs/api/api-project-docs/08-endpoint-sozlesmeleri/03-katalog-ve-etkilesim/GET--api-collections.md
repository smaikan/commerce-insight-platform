# GET /api/collections

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Kaynağı veya filtrelenmiş kaynak listesini okur.
- Operation ID: `GET-/api/collections`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `PageNumber` | query | Hayır | integer (int32) |
| `PageSize` | query | Hayır | integer (int32) |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

`CollectionDto` içindeki `imageUrl`, koleksiyona görsel tanımlanmadığında `null`, tanımlandığında en fazla 500 karakterlik görsel URL’sidir.

## Başarılı response (200)

Sayfalı `CollectionDto` sonucu döner.

