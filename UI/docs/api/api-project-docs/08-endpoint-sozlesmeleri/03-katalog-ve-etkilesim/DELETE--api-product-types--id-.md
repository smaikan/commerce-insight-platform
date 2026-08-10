# DELETE /api/product-types/{id}

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Ürün türünü ürünlerden bağımsız olarak fiziksel siler.
- Operation ID: `DELETE-/api/product-types/{id}`
- Yetki: `AdminOnly`.
- Content-Type: request body yoktur.
- Hata: 400 validation, 401 authentication, 403 policy, 404 ürün türü bulunamadı. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `id` | path | Evet | string (uuid) |

## Başarılı response (204)

Response body yoktur. Türe bağlı ürünler silinmez; bu ürünlerin `typeId` alanı veritabanında `null` yapılır.
