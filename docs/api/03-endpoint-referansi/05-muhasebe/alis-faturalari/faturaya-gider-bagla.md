# POST /api/accounting/purchase-invoices/{id}/expenses

- Görev alanı: **Muhasebe → Alış faturaları**.
- İşlev: Faturaya gider bağlar.
- Operation ID: `POST-/api/accounting/purchase-invoices/{id}/expenses`
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
| `categoryId` | string (uuid) | Evet |
| `allocationMethod` | integer (int32) | Evet |
| `amountExcludingVat` | number (double) | Evet |
| `vatRate` | number (double) | Evet |
| `description` | string | Hayır |
| `manualAllocations` | array | Hayır |

```json
{
  "categoryId": "00000000-0000-0000-0000-000000000001",
  "allocationMethod": 1,
  "amountExcludingVat": 1,
  "vatRate": 1,
  "description": "string",
  "manualAllocations": [
    {
      "purchaseInvoiceLineId": "00000000-0000-0000-0000-000000000001",
      "amountExcludingVat": 1
    }
  ]
}
```

## Başarılı response (200)

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "purchaseInvoiceId": "00000000-0000-0000-0000-000000000001",
  "categoryId": "00000000-0000-0000-0000-000000000001",
  "allocationMethod": 1,
  "amountExcludingVat": 1,
  "amountIncludingVat": 1,
  "allocations": [
    {
      "lineId": "00000000-0000-0000-0000-000000000001",
      "amountExcludingVat": 1,
      "amountIncludingVat": 1
    }
  ]
}
```

