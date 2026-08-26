# PUT /api/products/{id}/relations

- Görev alanı: **Katalog → Ürünler → Yönetim**.
- İşlev: Ürün ilişkilerini günceller.
- Operation ID: `PUT-/api/products/{id}/relations`
- Yetki: **Admin**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `id` | path | Evet | string |

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `collections` | string array | Evet |
| `bundleItems` | array | Evet |
| `tags` | array | Hayır |

```json
{
  "collections": [
    "Yaz Koleksiyonu"
  ],
  "bundleItems": [
    {
      "productId": "string",
      "quantity": 1
    }
  ],
  "tags": [
    "string"
  ]
}
```

`collections` ve `tags` isimle gönderilir. Bulunmayan collection veya tag API tarafından oluşturulur.

## Başarılı response (200)

Response body yoktur.

