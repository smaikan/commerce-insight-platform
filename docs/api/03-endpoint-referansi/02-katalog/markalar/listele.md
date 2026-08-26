# GET /api/brands

- Görev alanı: **Katalog → Markalar**.
- İşlev: listeler.
- Operation ID: `GET-/api/brands`
- Yetki: **Public**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `PageNumber` | query | Hayır | integer (int32) |
| `PageSize` | query | Hayır | integer (int32) |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

`BrandDtoPagedResult` döner. `imageUrl` opsiyoneldir ve görsel atanmamışsa `null` olur.

```json
{
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000001",
      "name": "Örnek Marka",
      "description": "Marka açıklaması",
      "url": "ornek-marka",
      "isActive": true,
      "imageUrl": "https://cdn.example.com/brands/ornek-marka.png"
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

