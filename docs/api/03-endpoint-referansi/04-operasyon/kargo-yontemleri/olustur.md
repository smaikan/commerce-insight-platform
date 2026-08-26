# POST /api/shipping-methods

- Görev alanı: **Operasyon → Kargo yöntemleri**.
- İşlev: oluşturur.
- Operation ID: `POST-/api/shipping-methods`
- Yetki: **Admin**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `name` | string | Evet |
| `fixedFee` | number (double) | Evet |
| `isActive` | boolean | Evet |
| `displayOrder` | integer (int32) | Evet |

```json
{
  "name": "string",
  "fixedFee": 1,
  "isActive": true,
  "displayOrder": 1
}
```

## Başarılı response (200)

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "name": "string",
  "fixedFee": 1,
  "isActive": true,
  "displayOrder": 1,
  "createdAt": "2026-07-29T12:00:00Z",
  "updatedAt": "2026-07-29T12:00:00Z"
}
```

