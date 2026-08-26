# GET /api/accounting/cash-accounts

- Görev alanı: **Muhasebe → Kasa hesapları**.
- İşlev: listeler.
- Operation ID: `GET-/api/accounting/cash-accounts`
- Yetki: **Admin**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000001",
    "code": "string",
    "name": "string",
    "currencyCode": "string",
    "isActive": true,
    "balance": 1
  }
]
```

