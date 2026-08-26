# GET /api/contact-messages/{id}

- Görev alanı: **Mağaza ve iletişim → İletişim yönetimi**.

- Yetki: **Admin**.

AdminOnly. 200 `ContactMessageDetailDto`; 404 `resource_not_found`. Detail tam message/phone, numeric subject/status, `concurrencyToken`, provided/verified order ayrımı, public assignee/user ID, append-only activities ve replies taşır.

Reply delivery status numeric: `0 Queued`, `1 Sent`, `2 Retrying`, `3 DeadLetter`. Status SMTP tamamlanmadan Sent olmaz.

Alanların required/nullable sözleşmesi için [ortak sözleşme](ortak-sozlesme.md#contactmessagedetaildto) tablosuna bakın.

## Başarılı response — 200

```json
{
  "id": "11111111-1111-1111-1111-111111111111",
  "referenceNumber": "CNT-0123456789ABCDEF0123",
  "userId": "U00007",
  "name": "Ada Lovelace",
  "email": "ada@example.com",
  "phone": "+90 555 000 00 00",
  "subject": 0,
  "providedOrderNumber": "ORD-2026-00042",
  "verifiedOrderId": "22222222-2222-2222-2222-222222222222",
  "isOrderVerified": true,
  "message": "Siparişim hakkında ayrıntılı destek rica ediyorum.",
  "status": 2,
  "assignedAdminUserId": "U00016",
  "createdAt": "2026-08-21T12:00:00Z",
  "updatedAt": "2026-08-21T12:10:00Z",
  "firstRespondedAt": "2026-08-21T12:10:00Z",
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
      "type": 4,
      "actorAdminUserId": "U00016",
      "content": null,
      "previousValue": null,
      "newValue": null,
      "replyId": "55555555-5555-5555-5555-555555555555",
      "createdAt": "2026-08-21T12:10:00Z"
    },
    {
      "id": "40000000-0000-0000-0000-000000000003",
      "type": 1,
      "actorAdminUserId": "U00016",
      "content": null,
      "previousValue": "New",
      "newValue": "WaitingForCustomer",
      "replyId": null,
      "createdAt": "2026-08-21T12:10:00Z"
    }
  ],
  "replies": [
    {
      "id": "55555555-5555-5555-5555-555555555555",
      "adminUserId": "U00016",
      "body": "Talebinizi aldık ve incelemeye başladık.",
      "deliveryStatus": 0,
      "createdAt": "2026-08-21T12:10:00Z"
    }
  ]
}
```

## Activity/audit sözleşmesi

Activity type numeric değerleri: `0 Submitted`, `1 StatusChanged`, `2 AssignmentChanged`, `3 InternalNoteAdded`, `4 ReplyQueued`.

| Type | `actorAdminUserId` | `content` | `previousValue` / `newValue` | `replyId` |
| --- | --- | --- | --- | --- |
| `Submitted (0)` | `null` | `null` | `null` / `null` | `null` |
| `StatusChanged (1)` | İşlemi yapan `U...` public admin ID | `null` | Numeric değer değil önceki/yeni enum adı; ör. `New` / `InProgress` | `null` |
| `AssignmentChanged (2)` | İşlemi yapan `U...` public admin ID | `null` | Önceki/yeni `U...` public assignee ID; atamasız taraf veya atama kaldırma `null` | `null` |
| `InternalNoteAdded (3)` | İşlemi yapan `U...` public admin ID | Dahili not yalnız bu alandadır | `null` / `null` | `null` |
| `ReplyQueued (4)` | İşlemi yapan `U...` public admin ID | `null` | `null` / `null` | İlişkili `ContactMessageReplyDto.id` |

`activities` dizisi `createdAt ASC`, ardından `id ASC`; `replies` dizisi de `createdAt ASC`, ardından `id ASC` sırasıyla döner. Admin timeline bu API sırasını kullanır.

## Response durumları

| HTTP | ProblemDetails `code` | Koşul |
| --- | --- | --- |
| `200` | — | Başarılı detail |
| `401` | `authentication_required`, `invalid_access_token` | Token yok/geçersiz |
| `403` | `forbidden` | Kullanıcı Admin değil |
| `404` | `resource_not_found` | ContactMessage bulunamadı |
