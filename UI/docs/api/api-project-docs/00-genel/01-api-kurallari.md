# API Genel Kuralları

## HTTP, kimlik ve güvenilir alanlar

API yolları `/api/...` ile başlar ve JSON kullanır. Üye/Admin istekleri `Authorization: Bearer <access-token>` taşır. User public ID `U...`, Product public ID `P...`; Order ve ProductVariant kimlikleri UUID’dir.

Frontend şu alanları hiçbir cart/checkout request’inde gönderemez: `UserId`, Product ID snapshot’ı, birim fiyat, vergi oranı/tutarı, indirim tutarı, kargo adı/ücreti, stok, ara/genel toplam, sipariş numarası veya durum. Bunların tamamı backend otoritesidir.

## ProblemDetails

```json
{
  "type": "urn:ecommerce:error:coupon_members_only",
  "title": "Coupon requires membership",
  "status": 409,
  "detail": "Bu kupon yalnızca üyeler içindir.",
  "instance": "/api/cart/checkout/guest",
  "code": "coupon_members_only",
  "traceId": "00-redacted",
  "timestamp": "2026-08-03T00:00:00Z"
}
```

Akışa özel ProblemDetails kodları:

| HTTP | code | UI davranışı |
| --- | --- | --- |
| 409 | `coupon_members_only` | “Bu kupon yalnızca üyeler içindir”; otomatik retry yapma |
| 409 | `idempotency_key_reused` | Aynı key farklı body ile kullanılmış; yeni intent oluştur veya eski body’ye dön |
| 413 | `payload_too_large` | Request body sınırı aşılmış; payload'ı küçült |
| 401/403/404 | `invalid_guest_access` | Session/CSRF/owner hatası; token veya sipariş varlığını ifşa etme |
| 428 | `guest_checkout_challenge_required` | Turnstile göster, yeni tokenla aynı intent/key’i tekrar gönder |
| 429 | `guest_checkout_rate_limited` | Kontrollü bekleme; hızlı otomatik retry yapma |
| 503 | `guest_checkout_protection_unavailable` | Formu koru, geçici hata göster; kontrolsüz bypass yapma |
| 428 | `contact_challenge_required` | Contact Turnstile göster; aynı body/key ile yeni token gönder |
| 429 | `contact_submission_rate_limited` | `Retry-After` kadar kontrollü bekle; otomatik hızlı retry yapma |
| 503 | `contact_protection_unavailable` | Contact formunu koru; Redis/Turnstile bypass etme |

## Pagination, concurrency ve idempotency

Sayfalı cevap `items`, `pageNumber`, `pageSize`, `totalCount`, `totalPages`, `hasPreviousPage`, `hasNextPage` alanlarını taşır. Varsayılan 1/20, üst sayfa boyutu çoğunlukla 100’dür.

Cart concurrency token, başarılı her add/update/remove/clear/merge/checkout temizliğiyle değişir. Her mutasyonda en son response’taki token kullanılmalıdır. `409 concurrency_conflict` sonrasında cart yeniden GET edilir; eski mutasyon körlemesine tekrarlanmaz.

Guest checkout ve payment için `Idempotency-Key` zorunludur. Aynı kullanıcı intent’inin network/timeout retry’ında aynı key ve aynı body korunur. Yeni intent yeni key alır. Guest checkout key’i guest cart kapsamında 24 saat saklanır; aktif kayıtta aynı body önceki Order’ı döndürür, farklı body `409 idempotency_key_reused` üretir.

Contact submission ve admin reply için de `Idempotency-Key` zorunludur. Contact status/assignment/note mutasyonlarında güncel `expectedConcurrencyToken` kullanılır; 409 sonrasında detail yeniden okunur ve kör overwrite yapılmaz.

## Rate limit

Üye politikaları değişmez: cart yaklaşık 60/dk, orders 30/dk, payments 10/dk. Guest checkout ayrıca yalnız guest endpointinde şu korumayı uygular:

- 10 dakikadaki üçüncü denemeden itibaren Turnstile;
- IP başına 15 dakikada 5 checkout;
- session/e-posta hash’i başına saatte 5 checkout;
- session/e-posta başına en fazla 3 aktif ödenmemiş rezervasyon;
- magic-link sipariş başına saatte 3, IP başına saatte 10.

Redis kesintisinde process içi fallback sayaç çalışır ve Turnstile zorunlu olur. Turnstile doğrulaması backend’den Cloudflare Siteverify’a yapılır; secret frontend’e verilmez.

Contact formunda Redis kesintisi fallback/bypass yapmaz ve 503 döner. Production Turnstile `action=contact_form` ile yapılandırılmış hostname'e bağlıdır. IP bazlı contact limiti yalnız açık `KnownProxies` kararıyla güvenilir forwarded zinciri doğrulanırsa açılır; aksi halde coarse BFF limiti ile normalize e-posta hash limiti birlikte kullanılır.

## BFF ve cache

Browser guest işlemlerini same-origin Next.js Route Handler üzerinden yapar. BFF yalnız allowlist edilmiş `ecommerce_guest_cart`, `ecommerce_guest_orders`, `ecommerce_guest_csrf`, `Idempotency-Key`, `X-Turnstile-Token`, `X-Guest-CSRF`, `Content-Type` ve gerekli correlation header’larını taşır. Upstream `Set-Cookie` storefront origin altında Secure/HttpOnly/SameSite=Lax olarak yeniden yazılır.

Server Component kendi Route Handler’ına HTTP çağrısı yapmaz; paylaşılan server-only API fonksiyonunu doğrudan çağırır. Cart, checkout, guest order ve order detail cevapları `no-store` olur. Cookie/token/PII loglanmaz.
