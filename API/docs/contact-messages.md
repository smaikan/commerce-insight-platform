# ContactMessage backend altyapısı

Storefront iletişim formu doğrudan SMTP çağırmaz. `POST /api/contact-messages`, `ContactMessage`, `ContactSubmissionIdempotency` ve `ContactMessageReceived` outbox kayıtlarını aynı serializable transaction içinde oluşturur ve `202 Accepted` döner. Worker mesaj gövdesini outbox payload'ında çoğaltmadan kaynak ContactMessage/Reply kaydından okur.

## Güvenlik ve idempotency

- `Idempotency-Key` zorunludur; raw değer saklanmaz veya loglanmaz. SHA-256 key hash ve canonical body fingerprint saklanır.
- Aynı key/body önceki `{referenceNumber,submittedAt}` receipt'ini döndürür; farklı body `409 idempotency_key_reused` üretir.
- Production'da Turnstile zorunludur; action `contact_form`, hostname `ContactProtection:Turnstile:Hostname` ile doğrulanır.
- Redis normalize e-posta hash limiti fail-closed'dur. Redis/Turnstile yoksa `503 contact_protection_unavailable`; limitte `429 contact_submission_rate_limited` ve `Retry-After` döner.
- IP limiti yalnız `ContactProtection:TrustForwardedClientIp=true` ve açık `ForwardedHeaders:KnownProxies` yapılandırmasıyla kullanılır. Diğer topolojide coarse BFF limiter, e-posta hash ve Turnstile birlikte çalışır.
- Anonim `orderNumber` yalnız provided metindir. Authenticated kullanıcıda owner-scoped order sorgusu eşleşirse `VerifiedOrderId` bağlanır; public response ve hatalar sipariş varlığını açıklamaz.

## Yönetim ve audit

AdminOnly liste/detail/status/assignment/note/reply endpointleri vardır. Status geçişleri domain allowlist'idir. Assignment, status, internal note ve reply queue işlemleri immutable `ContactMessageActivity` üretir. Not/reply edit veya delete edilmez. Raw User/Admin `long` kimliği API sözleşmesine açılmaz; `U...` public ID kullanılır.

`ContactMessageActivity.Id` ve `ContactMessageReply.Id` uygulamada üretilir ve EF modelinde `ValueGeneratedNever` olarak tanımlıdır. Tracked aggregate'a mutation sırasında eklenen child kayıtlar `Added/INSERT` state'iyle kalıcılaşır; `409 concurrency_conflict` yalnız aggregate concurrency token gerçekten stale olduğunda kullanılır.

Reply alıcısı request'ten alınmaz; ContactMessage e-postasından gelir. Reply ve deterministic `contact-reply:{replyId}` outbox kaydı aynı transaction'dadır. İlk reply `FirstRespondedAt` değerini set eder. Mevcut status `New` veya `InProgress` ise reply aynı transaction'da statusu `WaitingForCustomer` yapıp ayrı `StatusChanged` activity üretir; diğer statuslar değişmez. Reply her yeni intent'te `UpdatedAt` ve `ConcurrencyToken` değerini yeniler. SMTP tamamlanmadan Sent görünmez; detail DTO outbox alanlarından Queued/Sent/Retrying/DeadLetter türetir.

Alanların required/nullable durumu, tam başarılı JSON örnekleri, UTC-inclusive liste tarih filtreleri ve endpoint bazlı kesin ProblemDetails kodları frontend API sözleşmesindeki `14-iletisim-mesajlari` bölümünde yayımlanır.

## Konfigürasyon ve retention

- `Email:ContactInboxAddress`: operasyonel inbox.
- `Email:AdminContactMessageBaseUrl`: production'da non-loopback HTTPS admin detay kökü.
- `Email:SupportReplyToAddress`: müşteri reply e-postasının güvenli Reply-To adresi.
- `ContactPrivacy:NoticeVersion`, `NoticePublishedAtUtc`: server otoriteli privacy notice snapshot'ı.
- `ContactPrivacy:RetentionDays`: ürün kararıyla 60 gün (iki ay) olarak yapılandırılmıştır.
- Idempotency expiry 24 saattir ve günlük bounded batch cleanup yapılır.
- Retention worker başlangıçta ve günlük çalışır; `CreatedAt <= utcNow - RetentionDays` ve `AnonymizedAt is null` kayıtlarından en fazla `CleanupBatchSize` adet işler.
- Anonimleştirme kullanıcı/order bağını, ad, e-posta, telefon, provided order number ve mesaj gövdesini siler; internal note ile reply gövdelerini redakte eder. Status, subject, reference, activity tipi/aktörü/zamanı ve reply audit bağı korunur.
- Contact reply outbox alıcı PII değeri silinir. Henüz tamamlanmamış contact outbox teslimatları retention anında terminal dead-letter yapılır; böylece anonimleştirilmiş içerik sonradan gönderilmez.
- İşlem `AnonymizedAt` ile idempotenttir, aggregate ve outbox değişiklikleri serializable transaction içinde kalıcılaşır.

Migrationlar: `20260821160000_AddContactMessageManagement`, `20260821190646_AddContactMessageRetention`, `20260821222035_ConfigureContactChildIdsAsClientGenerated` (metadata-only, DDL yok).
