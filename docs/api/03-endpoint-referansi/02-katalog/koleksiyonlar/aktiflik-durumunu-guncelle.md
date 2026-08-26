# PATCH /api/collections/{id}/activation

- Görev alanı: **Katalog → Koleksiyonlar**.
- İşlev: Aktiflik durumunu günceller.
- Operation ID: `PATCH-/api/collections/{id}/activation`
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
| `isActive` | boolean | Evet |

```json
{
  "isActive": true
}
```

## Başarılı response (200)

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "name": "string",
  "description": "string",
  "url": "string",
  "isActive": true,
  "isFeatured": true,
  "displayOrder": 1
}
```

