# GET /api/users

- İşlev alanı: **02 Kullanıcı ve adres**
- İşlev: Kaynağı veya filtrelenmiş kaynak listesini okur.
- Operation ID: `GET-/api/users`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `PageNumber` | query | Hayır | integer (int32) |
| `PageSize` | query | Hayır | integer (int32) |
| `Search` | query | Hayır | string |
| `Role` | query | Hayır |  |
| `Status` | query | Hayır |  |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

`PagedResult<AdminUserDto>` döner. Atanabilir contact adminleri için `Role=2` ve `Status=1` filtreleri kullanılır. `AdminUserDto.id` raw numeric kimlik değil `U...` public ID'dir.
