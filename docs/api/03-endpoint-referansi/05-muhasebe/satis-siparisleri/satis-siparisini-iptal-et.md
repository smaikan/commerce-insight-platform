# POST /api/accounting/sales-orders/{id}/cancel

- Görev alanı: **Muhasebe → Satış siparişleri**.
- İşlev: Satış siparisini iptal eder.
- Operation ID: `POST-/api/accounting/sales-orders/{id}/cancel`
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
| `reason` | string | Evet |

```json
{
  "reason": "string"
}
```

## Başarılı response (200)

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "status": "string",
  "alreadyProcessed": true
}
```

