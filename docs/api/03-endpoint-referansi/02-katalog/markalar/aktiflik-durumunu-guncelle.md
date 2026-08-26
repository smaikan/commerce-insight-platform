# PATCH /api/brands/{id}/activation

- Görev alanı: **Katalog → Markalar**.
- İşlev: Aktiflik durumunu günceller.
- Operation ID: `PATCH-/api/brands/{id}/activation`
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
  "isActive": true,
  "imageUrl": "https://cdn.example.com/brands/ornek-marka.png"
}
```

## Başarılı response (200)

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "name": "string",
  "description": "string",
  "url": "string",
  "isActive": true
}
```

