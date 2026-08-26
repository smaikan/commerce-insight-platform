# PUT /api/product-images/{id}

- Görev alanı: **Katalog → Ürün görselleri**.
- İşlev: günceller.
- Operation ID: `PUT-/api/product-images/{id}`
- Yetki: **Admin**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `id` | path | Evet | string (uuid) |

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `imageUrl` | string | Evet |
| `altText` | string | Hayır |
| `displayOrder` | integer (int32) | Evet |
| `isMain` | boolean | Evet |

```json
{
  "imageUrl": "string",
  "altText": "string",
  "displayOrder": 1,
  "isMain": true
}
```

## Başarılı response (200)

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "productId": "string",
  "imageUrl": "string",
  "altText": "string",
  "displayOrder": 1,
  "isMain": true
}
```

