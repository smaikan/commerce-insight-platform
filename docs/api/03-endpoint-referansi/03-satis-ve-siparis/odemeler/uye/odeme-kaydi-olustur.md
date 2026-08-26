# POST /api/orders/{id}/payments

- Görev alanı: **Satış ve sipariş → Ödemeler → Üye işlemleri**.
- İşlev: Ödeme kaydi oluşturur.
- Operation ID: `POST-/api/orders/{id}/payments`
- Yetki: **User**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `id` | path | Evet | string (uuid) |
| `Idempotency-Key` | header | Hayır | string |

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `provider` | integer (int32) | Evet |

```json
{
  "provider": 0
}
```

## Başarılı response (200)

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "provider": 0,
  "status": 0,
  "amount": 1,
  "transactionId": "string",
  "paidAt": "2026-07-29T12:00:00Z",
  "createdAt": "2026-07-29T12:00:00Z"
}
```

