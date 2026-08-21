# PATCH /api/contact-messages/{id}/status

AdminOnly. Body:

```json
{"status":1,"expectedConcurrencyToken":"00000000-0000-0000-0000-000000000001"}
```

| Alan | Tip | Required | Nullable | Kural |
| --- | --- | --- | --- | --- |
| `status` | numeric enum | Evet | Hayır | Tanımlı status ve aşağıdaki izinli geçişlerden biri |
| `expectedConcurrencyToken` | UUID | Evet | Hayır | Son detail response'undaki güncel, empty olmayan token |

Domain allowlist durum geçişi ve immutable audit oluşturur.

| Mevcut durum | İzin verilen hedefler |
| --- | --- |
| `New (0)` | `InProgress (1)`, `WaitingForCustomer (2)`, `Closed (4)`, `Spam (5)` |
| `InProgress (1)` | `WaitingForCustomer (2)`, `Resolved (3)`, `Closed (4)`, `Spam (5)` |
| `WaitingForCustomer (2)` | `InProgress (1)`, `Resolved (3)`, `Closed (4)`, `Spam (5)` |
| `Resolved (3)` | `InProgress (1)`, `Closed (4)` |
| `Closed (4)` | `InProgress (1)` |
| `Spam (5)` | `New (0)`, `Closed (4)` |

Aynı durumdan aynı duruma geçiş geçersizdir. Başarı güncel `ContactMessageDetailDto` ve yenilenmiş `concurrencyToken` döndürür. Alan sözleşmesi için [README](README.md#contactmessagedetaildto) tablosuna bakın.

## Başarılı response — 200

```json
{
  "id": "11111111-1111-1111-1111-111111111111",
  "referenceNumber": "CNT-0123456789ABCDEF0123",
  "userId": null,
  "name": "Ada Lovelace",
  "email": "ada@example.com",
  "phone": null,
  "subject": 0,
  "providedOrderNumber": null,
  "verifiedOrderId": null,
  "isOrderVerified": false,
  "message": "Siparişim hakkında ayrıntılı destek rica ediyorum.",
  "status": 1,
  "assignedAdminUserId": null,
  "createdAt": "2026-08-21T12:00:00Z",
  "updatedAt": "2026-08-21T12:05:00Z",
  "firstRespondedAt": null,
  "resolvedAt": null,
  "closedAt": null,
  "concurrencyToken": "33333333-3333-3333-3333-333333333333",
  "privacyNoticeVersion": "2026-08-v1",
  "privacyNoticePublishedAt": "2026-08-21T00:00:00Z",
  "activities": [
    {"id":"40000000-0000-0000-0000-000000000001","type":0,"actorAdminUserId":null,"content":null,"previousValue":null,"newValue":null,"replyId":null,"createdAt":"2026-08-21T12:00:00Z"},
    {"id":"40000000-0000-0000-0000-000000000002","type":1,"actorAdminUserId":"U00016","content":null,"previousValue":"New","newValue":"InProgress","replyId":null,"createdAt":"2026-08-21T12:05:00Z"}
  ],
  "replies": []
}
```

## Response durumları

| HTTP | ProblemDetails `code` | Koşul |
| --- | --- | --- |
| `200` | — | Durum değişti |
| `400` | `validation_error`, `bad_request` | Enum/token/body doğrulaması veya binding hatası |
| `400` | `business_rule_violation` | Matriste olmayan ya da aynı duruma geçiş |
| `401` | `authentication_required`, `invalid_access_token` | Token yok/geçersiz |
| `403` | `forbidden` | Kullanıcı Admin değil |
| `404` | `resource_not_found` | ContactMessage bulunamadı |
| `409` | `concurrency_conflict` | Token stale; GET yapıp güncel veriyi alın, kör overwrite/retry yapmayın |
