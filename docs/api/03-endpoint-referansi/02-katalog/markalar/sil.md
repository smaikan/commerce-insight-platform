# DELETE /api/brands/{id}

- Görev alanı: **Katalog → Markalar**.
- İşlev: Markayı ürünlerden bağımsız olarak fiziksel siler.
- Operation ID: `DELETE-/api/brands/{id}`
- Yetki: **Admin**.
- Content-Type: request body yoktur.
- Hata: 400 validation, 401 authentication, 403 policy, 404 marka bulunamadı. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `id` | path | Evet | string (uuid) |

## Başarılı response (204)

Response body yoktur. Markaya bağlı ürünler silinmez; bu ürünlerin `brandId` alanı veritabanında `null` yapılır.
