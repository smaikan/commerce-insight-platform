# POST /api/product-engagement/products/{productId}/reviews

- Görev alanı: **Katalog → Ürün etkileşimleri → Yorumlar**.
- İşlev: Ürün yorumu yazar.
- Operation ID: `POST-/api/product-engagement/products/{productId}/reviews`
- Yetki: **User**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `productId` | path | Evet | string |

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `comment` | string | Evet |
| `title` | string | Hayır |
| `ratingValue` | integer (int32) | Hayır |

```json
{
  "comment": "string",
  "title": "string",
  "ratingValue": 1
}
```

## Başarılı response (200)

Response body yoktur.

