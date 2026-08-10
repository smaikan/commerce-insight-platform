# DELETE /api/tags/{id}

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Etiketi ürünlerden bağımsız olarak fiziksel siler.
- Operation ID: `DELETE-/api/tags/{id}`
- Yetki: `AdminOnly`.
- Content-Type: request body yoktur.
- Hata: 400 validation, 401 authentication, 403 policy, 404 etiket bulunamadı. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `id` | path | Evet | string (uuid) |

## Başarılı response (204)

Response body yoktur. Etiketli ürünler silinmez; yalnız ilgili `ProductTag` bağlantıları cascade olarak kaldırılır.
