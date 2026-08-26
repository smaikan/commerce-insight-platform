# İletişim Mesajları

İletişim modülü public form gönderimi ile Admin gelen kutusunu birbirinden ayırır.

## Public mesaj gönderimi

```http
POST /api/contact-messages
Content-Type: application/json
Idempotency-Key: 7704d1d4-bd95-479a-9e3b-acafee961bf7
```

```json
{
  "name": "Deniz Yılmaz",
  "email": "deniz@example.com",
  "phone": "+905551112233",
  "subject": 1,
  "message": "ORD-20260826-A1B2 numaralı siparişim hakkında bilgi almak istiyorum.",
  "providedOrderNumber": "ORD-20260826-A1B2",
  "privacyNoticeVersion": "2026-08",
  "privacyNoticePublishedAt": "2026-08-01T00:00:00Z"
}
```

Başarı `202 Accepted` döndürür:

```json
{
  "id": "ca82f7ce-b9f0-45aa-b3dc-c0d69f9f957a",
  "referenceNumber": "MSG-20260826-A1B2C3",
  "status": 0,
  "createdAt": "2026-08-26T12:00:00Z"
}
```

Production ortamında Turnstile challenge istenebilir. `428 contact_challenge_required` sonrasında aynı intent/body/idempotency key, yeni `X-Turnstile-Token` ile gönderilir.

## Public güvenlik kuralları

- Yaklaşık 16 KB body sınırı vardır.
- Normalize e-posta ve ilgili limit kovaları uygulanır.
- `429` durumunda `Retry-After` dikkate alınır.
- Redis veya Turnstile gerekli olduğu hâlde erişilemiyorsa güvenlik bypass edilmez; `503 contact_protection_unavailable` döner.
- Turnstile secret hiçbir zaman frontend'e verilmez.

## Admin gelen kutusu

Admin endpointleri:

- `GET /api/contact-messages`
- `GET /api/contact-messages/{id}`
- `PATCH /api/contact-messages/{id}/status`
- `PATCH /api/contact-messages/{id}/assignment`
- `POST /api/contact-messages/{id}/notes`
- `POST /api/contact-messages/{id}/replies`

Status, assignment ve note işlemleri güncel `expectedConcurrencyToken` ister. Reply ayrıca `Idempotency-Key` kullanır.

İlk yanıt:

- `firstRespondedAt` değerini set eder.
- `New` veya `InProgress` mesajı otomatik `WaitingForCustomer` durumuna taşıyabilir.
- Reply ve gerekiyorsa status activity kaydı üretir.
- E-posta teslimatını asenkron outbox üzerinden gerçekleştirir.

## Retention

60 günlük retention işi müşteri PII'sini, internal note ve reply body'lerini anonimleştirir; audit tipi, aktör, zaman ve operasyon metadata'sı korunur. Anonimleştirilmiş kayda yeni reply gönderilemez.

## Ayrıntılı referans

[İletişim mesajı endpointleri](../03-endpoint-referansi/06-magaza-ve-iletisim/iletisim-yonetimi/README.md)

