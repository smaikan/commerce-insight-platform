# GET /api/brands/{id}

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Kaynağı veya filtrelenmiş kaynak listesini okur.
- Operation ID: `GET-/api/brands/{id}`
- Yetki: `AllowAnonymous` (Public).
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
    "name":  "string",
    "description":  "string",
    "url":  "string",
    "isActive":  true,
    "imageUrl": "https://cdn.example.com/brands/ornek-marka.png"
}
```

`imageUrl` opsiyoneldir; görsel atanmamış markada `null` döner.

