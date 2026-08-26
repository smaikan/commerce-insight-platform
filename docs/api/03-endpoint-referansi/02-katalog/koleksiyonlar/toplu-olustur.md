# POST /api/collections/bulk

- Görev alanı: **Katalog → Koleksiyonlar**.
- İşlev: Toplu oluşturur.
- Operation ID: `POST-/api/collections/bulk`
- Yetki: **Admin**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `collections` | array | Evet |

```json
{
  "collections": [
    {
      "name": "string",
      "url": "string",
      "description": "string",
      "isActive": true,
      "isFeatured": true,
      "displayOrder": 1,
      "imageUrl": "https://cdn.example.com/collections/yaz.jpg"
    }
  ]
}
```

Her `collections` öğesi isteğe bağlı, en fazla 500 karakterlik `imageUrl` alanı kabul eder.

## Başarılı response (201)

Oluşturulan `CollectionDto[]` dizisi döner; her DTO isteğe bağlı `imageUrl` alanını içerir.

