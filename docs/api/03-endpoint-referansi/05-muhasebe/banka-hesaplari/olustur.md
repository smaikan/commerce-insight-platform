# POST /api/accounting/bank-accounts

- Görev alanı: **Muhasebe → Banka hesapları**.
- İşlev: oluşturur.
- Operation ID: `POST-/api/accounting/bank-accounts`
- Yetki: **Admin**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `code` | string | Evet |
| `name` | string | Evet |
| `bankName` | string | Evet |
| `iban` | string | Hayır |
| `currencyCode` | string | Evet |

```json
{
  "code": "string",
  "name": "string",
  "bankName": "string",
  "iban": "string",
  "currencyCode": "string"
}
```

## Başarılı response (200)

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "code": "string",
  "name": "string",
  "bankName": "string",
  "iban": "string",
  "currencyCode": "string",
  "isActive": true,
  "balance": 1
}
```

