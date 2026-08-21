# PATCH /api/contact-messages/{id}/assignment

AdminOnly. Body `{ "assignedAdminUserId":"U00016", "expectedConcurrencyToken":"33333333-3333-3333-3333-333333333332" }`; `null` atamayı kaldırır. Yalnız aktif Admin atanabilir. Atanabilir adminler mevcut `GET /api/users?role=2&status=1` sözleşmesinden alınır. Yeni assignee endpoint'i yoktur.

| Alan | Tip | Required | Nullable | Kural |
| --- | --- | --- | --- | --- |
| `assignedAdminUserId` | `U...` string | Hayır | Evet | Aktif Admin public ID; `null` atamayı kaldırır |
| `expectedConcurrencyToken` | UUID | Evet | Hayır | Son detail response'undaki güncel token |

Aynı assignee tekrar gönderilirse no-op olarak güncel detail döner; activity/token değişmez. Gerçek değişiklik `AssignmentChanged` activity üretir ve tokenı yeniler.

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
  "status": 0,
  "assignedAdminUserId": "U00016",
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
    {"id":"40000000-0000-0000-0000-000000000002","type":2,"actorAdminUserId":"U00016","content":null,"previousValue":null,"newValue":"U00016","replyId":null,"createdAt":"2026-08-21T12:05:00Z"}
  ],
  "replies": []
}
```

## Response durumları

| HTTP | ProblemDetails `code` | Koşul |
| --- | --- | --- |
| `200` | — | Atama değişti/kaldırıldı veya aynı atama no-op döndü |
| `400` | `validation_error`, `bad_request` | Public ID/token/body doğrulaması veya binding hatası |
| `401` | `authentication_required`, `invalid_access_token` | Token yok/geçersiz |
| `403` | `forbidden` | Kullanıcı Admin değil |
| `404` | `resource_not_found` | ContactMessage bulunamadı |
| `409` | `concurrency_conflict` | Token stale |
| `409` | `conflict` | Hedef kullanıcı yok, Admin değil veya aktif değil |
