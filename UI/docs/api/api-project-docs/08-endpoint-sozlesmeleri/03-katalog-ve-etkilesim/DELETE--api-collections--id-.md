# DELETE /api/collections/{id}

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Koleksiyonu ürünlerden bağımsız olarak fiziksel siler.
- Operation ID: `DELETE-/api/collections/{id}`
- Yetki: `AdminOnly`.
- Content-Type: request body yoktur.
- Hata: 400 validation, 401 authentication, 403 policy, 404 koleksiyon bulunamadı. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `id` | path | Evet | string (uuid) |

## Başarılı response (204)

Response body yoktur. Koleksiyondaki ürünler silinmez; yalnız ilgili `ProductCollection` bağlantıları cascade olarak kaldırılır.
