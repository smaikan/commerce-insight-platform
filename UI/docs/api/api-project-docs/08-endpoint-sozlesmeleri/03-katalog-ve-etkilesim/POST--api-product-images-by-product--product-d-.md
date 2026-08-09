# POST /api/product-images/by-product/{productId}

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Yeni kaynak veya iş akışı adımı oluşturur/başlatır.
- Operation ID: `POST-/api/product-images/by-product/{productId}`
- Yetki: kesin `AllowAnonymous` / `User` / `AdminOnly` bilgisi için `../../08-controller-kapsam-denetimi.md` kontrol edilmelidir.
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
| `imageUrl` | string | Evet |
| `altText` | string | Hayır |
| `displayOrder` | integer (int32) | Evet |
| `isMain` | boolean | Evet |

```json
{
    "imageUrl":  "string",
    "altText":  "string",
    "displayOrder":  1,
    "isMain":  true
}
```

## Başarılı response (200)

```json
{
    "id":  "00000000-0000-0000-0000-000000000001",
    "productId":  "string",
    "imageUrl":  "string",
    "altText":  "string",
    "displayOrder":  1,
    "isMain":  true
}
```

