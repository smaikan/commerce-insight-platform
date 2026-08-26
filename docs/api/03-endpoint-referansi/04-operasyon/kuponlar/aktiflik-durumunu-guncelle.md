# PATCH /api/coupons/{id}/activation

- Görev alanı: **Operasyon → Kuponlar**.
- İşlev: Aktiflik durumunu günceller.
- Operation ID: `PATCH-/api/coupons/{id}/activation`
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
  "code": "string",
  "description": "string",
  "discountType": 0,
  "discountValue": 1,
  "minimumOrderAmount": 1,
  "usageLimit": 1,
  "usedCount": 1,
  "startsAt": "2026-07-29T12:00:00Z",
  "expiresAt": "2026-07-29T12:00:00Z",
  "isActive": true,
  "createdAt": "2026-07-29T12:00:00Z",
  "updatedAt": "2026-07-29T12:00:00Z"
}
```

