# GET /api/contact-messages

AdminOnly. `PagedResult<ContactMessageSummaryDto>` döndürür.

## Query

| Parametre | Tip | Required | Nullable | Kural |
| --- | --- | --- | --- | --- |
| `pageNumber` | integer | Hayır | Hayır | Varsayılan `1`, aralık 1–10000 |
| `pageSize` | integer | Hayır | Hayır | Varsayılan `20`, aralık 1–100 |
| `search` | string | Hayır | Evet | Max 200; reference number, name, email ve provided order number alanlarında arar; message aranmaz |
| `status` | numeric enum | Hayır | Evet | Tanımlı status değerlerinden biri |
| `subject` | numeric enum | Hayır | Evet | Tanımlı subject değerlerinden biri |
| `assignedAdminUserId` | `U...` string | Hayır | Evet | Public admin ID |
| `createdFromUtc` | UTC date-time | Hayır | Evet | Inclusive alt sınır: `createdAt >= createdFromUtc` |
| `createdToUtc` | UTC date-time | Hayır | Evet | Inclusive üst sınır: `createdAt <= createdToUtc` |

Tarih filtreleri `Z`/UTC olarak gönderilmelidir; local veya offset'i UTC olmayan değerler `400 validation_error` üretir. İki tarih birlikte verildiğinde `createdFromUtc <= createdToUtc` olmalıdır. Sıra garantisi `createdAt DESC`, ardından `id DESC`tir. Özet tam `message` ve `phone` taşımaz. Summary alanları için [README](README.md#contactmessagesummarydto) sözleşmesine bakın.

## Başarılı response — 200

```json
{
  "items": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "referenceNumber": "CNT-0123456789ABCDEF0123",
      "name": "Ada Lovelace",
      "email": "ada@example.com",
      "subject": 0,
      "status": 1,
      "providedOrderNumber": "ORD-2026-00042",
      "hasVerifiedOrder": true,
      "assignedAdminUserId": "U00016",
      "createdAt": "2026-08-21T12:00:00Z",
      "updatedAt": "2026-08-21T12:05:00Z"
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

## Response durumları

| HTTP | ProblemDetails `code` | Koşul |
| --- | --- | --- |
| `200` | — | Başarılı sayfalı liste |
| `400` | `validation_error` | Aralık, enum, UTC veya tarih sırası validasyonu |
| `400` | `bad_request` | Query değeri model binding ile çözümlenemedi |
| `401` | `authentication_required`, `invalid_access_token` | Token yok/geçersiz |
| `403` | `forbidden` | Kullanıcı Admin değil |
