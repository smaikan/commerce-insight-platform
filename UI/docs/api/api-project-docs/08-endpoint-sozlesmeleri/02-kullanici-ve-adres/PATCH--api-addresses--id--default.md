# PATCH /api/addresses/{id}/default

- İşlev alanı: **02 Kullanıcı ve adres**
- İşlev: Kaynağın belirli durum veya alanlarını değiştirir.
- Operation ID: `PATCH-/api/addresses/{id}/default`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `id` | path | Evet | string (uuid) |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

```json
{
    "id":  "00000000-0000-0000-0000-000000000001",
    "type":  0,
    "title":  "string",
    "firstName":  "string",
    "lastName":  "string",
    "phoneNumber":  "string",
    "city":  "string",
    "district":  "string",
    "fullAddress":  "string",
    "postalCode":  "string",
    "isDefault":  true,
    "createdAt":  "2026-07-29T12:00:00Z",
    "updatedAt":  "2026-07-29T12:00:00Z"
}
```

