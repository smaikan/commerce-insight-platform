# POST /api/contact-messages/{id}/replies

AdminOnly. Alıcı body'den değil `ContactMessage.email` alanından türetilir.

| Alan/header | Tip | Required | Nullable | Kural |
| --- | --- | --- | --- | --- |
| `Idempotency-Key` header | string | Evet | Hayır | Max 200; control karakteri içermez |
| `body` | string | Evet | Hayır | 1–5000, güvenli düz metin |

Request body:

```json
{"body":"Talebinizi aldık ve incelemeye başladık."}
```

## Yan etkiler

- `ContactMessageReply`, `ReplyQueued` activity ve deduplication key'i `contact-reply:{replyId}` olan e-posta outbox kaydı aynı transaction'da oluşturulur.
- `firstRespondedAt` yalnız ilk reply kuyruğa alınırken set edilir; sonraki reply'lar bu değeri değiştirmez.
- Mevcut status `New (0)` veya `InProgress (1)` ise otomatik `WaitingForCustomer (2)` yapılır ve ayrıca `StatusChanged` activity oluşur.
- Mevcut status `WaitingForCustomer`, `Resolved`, `Closed` veya `Spam` ise reply statusu otomatik değiştirmez.
- Reply mutation'ı `updatedAt` ve `concurrencyToken` değerini yeniler. Sonraki status/assignment/note çağrısında response'taki yeni token kullanılmalıdır.
- `202`, SMTP teslim edildi demek değildir. Yeni reply önce `Queued (0)` görünür; worker sonucuna göre `Sent (1)`, `Retrying (2)` veya `DeadLetter (3)` olur.
- Aynı key/body replay'i ikinci reply/outbox/e-posta üretmez ve yine güncel detail döner; aynı key/farklı body `409 idempotency_key_reused` üretir.

## Başarılı response — 202

```json
{
  "id": "11111111-1111-1111-1111-111111111111",
  "referenceNumber": "CNT-0123456789ABCDEF0123",
  "userId": "U00007",
  "name": "Ada Lovelace",
  "email": "ada@example.com",
  "phone": null,
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
    {"id":"40000000-0000-0000-0000-000000000001","type":0,"actorAdminUserId":null,"content":null,"previousValue":null,"newValue":null,"replyId":null,"createdAt":"2026-08-21T12:00:00Z"},
    {"id":"40000000-0000-0000-0000-000000000002","type":4,"actorAdminUserId":"U00016","content":null,"previousValue":null,"newValue":null,"replyId":"55555555-5555-5555-5555-555555555555","createdAt":"2026-08-21T12:10:00Z"},
    {"id":"40000000-0000-0000-0000-000000000003","type":1,"actorAdminUserId":"U00016","content":null,"previousValue":"New","newValue":"WaitingForCustomer","replyId":null,"createdAt":"2026-08-21T12:10:00Z"}
  ],
  "replies": [
    {"id":"55555555-5555-5555-5555-555555555555","adminUserId":"U00016","body":"Talebinizi aldık ve incelemeye başladık.","deliveryStatus":0,"createdAt":"2026-08-21T12:10:00Z"}
  ]
}
```

## Response durumları

| HTTP | ProblemDetails `code` | Koşul |
| --- | --- | --- |
| `202` | — | Reply kuyruğa alındı veya aynı key/body replay edildi |
| `400` | `validation_error` | Body veya Idempotency-Key validasyonu |
| `400` | `bad_request` | Malformed JSON/header veya model binding hatası |
| `400` | `business_rule_violation` | Retention ile anonimleştirilmiş contact kaydına reply gönderilmeye çalışıldı |
| `401` | `authentication_required`, `invalid_access_token` | Token yok/geçersiz |
| `403` | `forbidden` | Kullanıcı Admin değil |
| `404` | `resource_not_found` | ContactMessage bulunamadı |
| `409` | `idempotency_key_reused` | Aynı key farklı reply body ile kullanıldı |
