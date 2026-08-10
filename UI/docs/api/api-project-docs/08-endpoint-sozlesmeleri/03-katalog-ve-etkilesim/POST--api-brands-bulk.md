# POST /api/brands/bulk

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Yeni kaynak veya iş akışı adımı oluşturur/başlatır.
- Operation ID: `POST-/api/brands/bulk`
- Yetki: `AdminOnly`.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `brands` | array | Evet |

Her `brands` öğesi `name`, `url`, `description`, `isActive` ve opsiyonel `imageUrl` alanlarını taşır. `imageUrl` en fazla 500 karakterdir; atlanır, `null` veya boş gönderilirse ilgili marka görselsiz oluşturulur.

```json
{
  "brands": [
    {
      "name": "Örnek Marka",
      "url": "ornek-marka",
      "description": "Marka açıklaması",
      "isActive": true,
      "imageUrl": "https://cdn.example.com/brands/ornek-marka.png"
    }
  ]
}
```

## Başarılı response (201)

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000001",
    "name": "Örnek Marka",
    "description": "Marka açıklaması",
    "url": "ornek-marka",
    "isActive": true,
    "imageUrl": "https://cdn.example.com/brands/ornek-marka.png"
  }
]
```

