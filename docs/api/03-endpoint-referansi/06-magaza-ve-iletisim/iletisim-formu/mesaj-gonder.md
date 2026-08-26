# POST /api/contact-messages

- Görev alanı: **Mağaza ve iletişim → İletişim formu**.

- Yetki: **Public + form koruması**.

Public iletişim formunu kalıcı kayda ve outbox'a kabul eder. OpenAPI security boş olmalıdır. `Idempotency-Key` zorunlu ve max 200; production'da `X-Turnstile-Token` zorunlu ve max 2048'dir. Body yaklaşık 16 KB ile sınırlıdır.

## Request

| Alan/header | Tip | Required | Nullable | Kural |
| --- | --- | --- | --- | --- |
| `Idempotency-Key` header | string | Evet | Hayır | Max 200; control karakteri içermez |
| `X-Turnstile-Token` header | string | Production'da evet | Evet | Max 2048; `action=contact_form` ve configured hostname doğrulanır |
| `name` | string | Evet | Hayır | 2–150, güvenli düz metin |
| `email` | email string | Evet | Hayır | Max 320; normalize edilerek saklanır |
| `phone` | string | Hayır | Evet | Max 30 |
| `subject` | numeric enum | Evet | Hayır | `0`–`5`, tanımlı değer olmalı |
| `orderNumber` | string | Hayır | Evet | Max 50; tek başına sipariş sahipliği kanıtı değildir |
| `message` | string | Evet | Hayır | 20–5000, düz metin |

```json
{
  "name": "Ada Lovelace",
  "email": "ada@example.com",
  "phone": null,
  "subject": 0,
  "orderNumber": "ORD-...",
  "message": "Siparişim hakkında destek rica ediyorum."
}
```

Alan sınırları: name 2–150, email required/max 320, phone nullable/max 30, orderNumber nullable/max 50, message 20–5000. HTML, NUL ve tehlikeli kontrol karakterleri reddedilir.

## Başarılı response — 202

```json
{
  "referenceNumber": "CNT-0123456789ABCDEF0123",
  "submittedAt": "2026-08-21T12:00:00Z"
}
```

Response GUID, status, user ID veya PII taşımaz. Aynı key/body aynı receipt; aynı key/farklı body `409 idempotency_key_reused`. Ayrıca 400, request sınırında 413 `payload_too_large`, 428 `contact_challenge_required`, 429 `contact_submission_rate_limited` + `Retry-After`, 503 `contact_protection_unavailable` ProblemDetails dönebilir. Sipariş varlığı açıklanmaz.

Başvuru, `ContactMessage` ile operasyonel inbox bildirim outbox kaydını aynı transaction'da oluşturur. Request sırasında SMTP çağrılmaz; ilk sürümde müşteriye otomatik acknowledgment e-postası gönderilmez.

## Response durumları

| HTTP | ProblemDetails `code` | Koşul |
| --- | --- | --- |
| `202` | — | Başvuru kabul edildi veya aynı key/body replay edildi |
| `400` | `validation_error` | Body/header alan validasyonu |
| `400` | `bad_request` | Malformed JSON veya model binding hatası |
| `409` | `idempotency_key_reused` | Aynı key farklı canonical body ile kullanıldı |
| `409` | `conflict` | Sınırlı denemede benzersiz reference number üretilemedi |
| `413` | `payload_too_large` | Yaklaşık 16 KB request sınırı aşıldı |
| `428` | `contact_challenge_required` | Turnstile token eksik/geçersiz/süresi dolmuş |
| `429` | `contact_submission_rate_limited` | Coarse, e-posta hash veya güvenilir IP limiti; `Retry-After` döner |
| `503` | `contact_protection_unavailable` | Redis/Turnstile protection kullanılamıyor |
