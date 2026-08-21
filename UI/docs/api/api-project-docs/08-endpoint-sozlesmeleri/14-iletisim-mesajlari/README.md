# 14 İletişim mesajları

- [POST /api/contact-messages](POST--api-contact-messages.md)
- [GET /api/contact-messages](GET--api-contact-messages.md)
- [GET /api/contact-messages/{id}](GET--api-contact-messages--id-.md)
- [PATCH /api/contact-messages/{id}/status](PATCH--api-contact-messages--id--status.md)
- [PATCH /api/contact-messages/{id}/assignment](PATCH--api-contact-messages--id--assignment.md)
- [POST /api/contact-messages/{id}/notes](POST--api-contact-messages--id--notes.md)
- [POST /api/contact-messages/{id}/replies](POST--api-contact-messages--id--replies.md)

Subject numeric wire değerleri: `0 OrderSupport`, `1 ProductInformation`, `2 ReturnOrCancellationSupport`, `3 CorporateOrWholesale`, `4 FeedbackOrComplaint`, `5 Other`. Status: `0 New`, `1 InProgress`, `2 WaitingForCustomer`, `3 Resolved`, `4 Closed`, `5 Spam`. Veritabanında enumlar string saklanır.

Activity type: `0 Submitted`, `1 StatusChanged`, `2 AssignmentChanged`, `3 InternalNoteAdded`, `4 ReplyQueued`. Koşullu activity alanları ve timeline sırası için detail sözleşmesine bakın.

## Response DTO sözleşmeleri

`Required`, alanın başarılı JSON response içinde zorunlu olduğunu; `Nullable`, alanın `null` olabileceğini belirtir. Tarihler ISO 8601 UTC (`...Z`), kimlikler aksi yazılmadıkça UUID'dir. `U...` değerleri raw `long` olmayan public user ID'leridir.

### `ContactMessageSummaryDto`

| Alan | Tip | Required | Nullable | Anlam |
| --- | --- | --- | --- | --- |
| `id` | UUID | Evet | Hayır | Admin contact mesaj kimliği |
| `referenceNumber` | string | Evet | Hayır | Kullanıcıya gösterilebilir `CNT-...` referansı |
| `name` | string | Evet | Hayır | Gönderen adı |
| `email` | string | Evet | Hayır | Normalize e-posta |
| `subject` | numeric enum | Evet | Hayır | Contact subject |
| `status` | numeric enum | Evet | Hayır | Contact status |
| `providedOrderNumber` | string | Hayır | Evet | Kullanıcının yazdığı, doğrulanmış sahiplik sayılmayan sipariş numarası |
| `hasVerifiedOrder` | boolean | Evet | Hayır | Üyeye ait sipariş doğrulanarak bağlandı mı |
| `assignedAdminUserId` | `U...` string | Hayır | Evet | Atanan admin; atama yoksa `null` |
| `createdAt` | UTC date-time | Evet | Hayır | Oluşturulma zamanı |
| `updatedAt` | UTC date-time | Hayır | Evet | Son mutation zamanı; henüz mutation yoksa `null` |

### `ContactMessageDetailDto`

| Alan | Tip | Required | Nullable | Anlam |
| --- | --- | --- | --- | --- |
| `id` | UUID | Evet | Hayır | Admin contact mesaj kimliği |
| `referenceNumber` | string | Evet | Hayır | Public başvuru referansı |
| `userId` | `U...` string | Hayır | Evet | Başvuru oturum açmış üyeye aitse public ID |
| `name` | string | Evet | Hayır | Gönderen adı |
| `email` | string | Evet | Hayır | Normalize e-posta |
| `phone` | string | Hayır | Evet | Girildiyse telefon |
| `subject` | numeric enum | Evet | Hayır | Contact subject |
| `providedOrderNumber` | string | Hayır | Evet | Kullanıcının girdiği sipariş numarası |
| `verifiedOrderId` | UUID | Hayır | Evet | Yalnız oturumdaki üyenin sahipliği doğrulandıysa bağlı sipariş |
| `isOrderVerified` | boolean | Evet | Hayır | `verifiedOrderId` varlığının açık göstergesi |
| `message` | string | Evet | Hayır | Tam düz metin mesaj |
| `status` | numeric enum | Evet | Hayır | Güncel contact durumu |
| `assignedAdminUserId` | `U...` string | Hayır | Evet | Atanan admin veya `null` |
| `createdAt` | UTC date-time | Evet | Hayır | Oluşturulma zamanı |
| `updatedAt` | UTC date-time | Hayır | Evet | Son mutation zamanı |
| `firstRespondedAt` | UTC date-time | Hayır | Evet | İlk reply kuyruğa alındığı zaman |
| `resolvedAt` | UTC date-time | Hayır | Evet | Son `Resolved` geçiş zamanı |
| `closedAt` | UTC date-time | Hayır | Evet | Son `Closed` geçiş zamanı |
| `concurrencyToken` | UUID | Evet | Hayır | Sonraki status/assignment/note mutation'ında gönderilecek token |
| `privacyNoticeVersion` | string | Evet | Hayır | Sunucu tarafında uygulanan aydınlatma metni sürümü |
| `privacyNoticePublishedAt` | UTC date-time | Evet | Hayır | Aydınlatma metni yayın tarihi |
| `activities` | `ContactMessageActivityDto[]` | Evet | Hayır | Append-only audit; boş dizi olabilir |
| `replies` | `ContactMessageReplyDto[]` | Evet | Hayır | Immutable reply kayıtları; boş dizi olabilir |

### `ContactMessageActivityDto`

| Alan | Tip | Required | Nullable | Anlam |
| --- | --- | --- | --- | --- |
| `id` | UUID | Evet | Hayır | Activity kimliği |
| `type` | numeric enum | Evet | Hayır | Activity tipi |
| `actorAdminUserId` | `U...` string | Hayır | Evet | `Submitted` için `null`, diğer tiplerde işlemi yapan admin |
| `content` | string | Hayır | Evet | Yalnız `InternalNoteAdded` not içeriği |
| `previousValue` | string | Hayır | Evet | Status enum adı veya assignment public ID'si; tipe göre `null` |
| `newValue` | string | Hayır | Evet | Status enum adı veya assignment public ID'si; tipe göre `null` |
| `replyId` | UUID | Hayır | Evet | Yalnız `ReplyQueued` için ilgili reply kimliği |
| `createdAt` | UTC date-time | Evet | Hayır | Activity zamanı |

### `ContactMessageReplyDto`

| Alan | Tip | Required | Nullable | Anlam |
| --- | --- | --- | --- | --- |
| `id` | UUID | Evet | Hayır | Reply kimliği; `ReplyQueued.replyId` bu değere bağlanır |
| `adminUserId` | `U...` string | Evet | Hayır | Yanıtı kuyruğa alan admin |
| `body` | string | Evet | Hayır | Müşteriye gönderilecek düz metin |
| `deliveryStatus` | numeric enum | Evet | Hayır | `0 Queued`, `1 Sent`, `2 Retrying`, `3 DeadLetter` |
| `createdAt` | UTC date-time | Evet | Hayır | Kuyruğa alınma zamanı |

## ProblemDetails sözleşmesi

Hata response'ları `application/problem+json` döner. Ortak alanlar `type`, `title`, `status`, `detail`, `instance`, `code`, `traceId`, `timestamp`; `validation_error` ayrıca alan bazlı `errors` taşır. Her endpoint dosyasındaki response tablosu o endpoint için mümkün kesin `code` değerlerini listeler. Admin endpointlerinde `401` kodu token yoksa `authentication_required`, token geçersiz veya süresi dolmuşsa `invalid_access_token`; `403` kodu `forbidden`; bulunamayan kaynak `404 resource_not_found` olur.

## Retention

`ContactPrivacy:RetentionDays=60` uygulanır. Günlük bounded worker, oluşturulmasından 60 gün geçen kayıtların müşteri PII alanlarını, internal note içeriğini ve reply gövdesini anonimleştirir; status/subject/reference ile activity tipi, aktörü, zamanı ve reply bağlantısı audit amacıyla korunur. Bekleyen contact e-postaları terminalleştirilir. Anonimleştirilmiş kayda reply gönderme girişimi `400 business_rule_violation` üretir.
