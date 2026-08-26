# GET /api/accounting/purchase-invoices/{id}/expenses

- Görev alanı: **Muhasebe → Alış faturaları**.
- İşlev: Faturanin giderlerini listeler.
- Operation ID: `GET-/api/accounting/purchase-invoices/{id}/expenses`
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
[
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
]
```

