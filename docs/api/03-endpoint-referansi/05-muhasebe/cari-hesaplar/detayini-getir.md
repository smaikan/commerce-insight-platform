# GET /api/accounting/current-accounts/{id}

- Görev alanı: **Muhasebe → Cari hesaplar**.
- İşlev: Detayını getirir.
- Operation ID: `GET-/api/accounting/current-accounts/{id}`
- Yetki: **Admin**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `id` | path | Evet | string (uuid) |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "code": "string",
  "type": 1,
  "name": "string",
  "tradeName": "string",
  "nationalIdentityNumber": "string",
  "taxNumber": "string",
  "taxOffice": "string",
  "phoneNumber": "string",
  "email": "string",
  "country": "string",
  "city": "string",
  "district": "string",
  "neighborhood": "string",
  "addressLine": "string",
  "postalCode": "string",
  "isActive": true,
  "userId": "string"
}
```




