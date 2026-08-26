# POST /api/contact-messages/{id}/notes

- Görev alanı: **Mağaza ve iletişim → İletişim yönetimi**.

- Yetki: **Admin**.

AdminOnly. Body `{ "note":"Dahili operasyon notu", "expectedConcurrencyToken":"33333333-3333-3333-3333-333333333332" }`. Note append-only'dir; edit/delete yoktur.

| Alan | Tip | Required | Nullable | Kural |
| --- | --- | --- | --- | --- |
| `note` | string | Evet | Hayır | 1–2000, güvenli düz metin |
| `expectedConcurrencyToken` | UUID | Evet | Hayır | Son detail response'undaki güncel token |

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
    {
      "id": "40000000-0000-0000-0000-000000000001",
      "type": 0,
      "actorAdminUserId": null,
      "content": null,
      "previousValue": null,
      "newValue": null,
      "replyId": null,
      "createdAt": "2026-08-21T12:00:00Z"
    },
    {
      "id": "40000000-0000-0000-0000-000000000002",
      "type": 3,
      "actorAdminUserId": "U00016",
      "content": "Dahili operasyon notu",
      "previousValue": null,
      "newValue": null,
      "replyId": null,
      "createdAt": "2026-08-21T12:05:00Z"
    }
  ],
  "replies": []
}
```

## Response durumları

| HTTP | ProblemDetails `code` | Koşul |
| --- | --- | --- |
| `200` | — | Not eklendi, detail ve yeni token döndü |
| `400` | `validation_error`, `bad_request` | Note/token/body doğrulaması veya binding hatası |
| `401` | `authentication_required`, `invalid_access_token` | Token yok/geçersiz |
| `403` | `forbidden` | Kullanıcı Admin değil |
| `404` | `resource_not_found` | ContactMessage bulunamadı |
| `409` | `concurrency_conflict` | Token stale |
