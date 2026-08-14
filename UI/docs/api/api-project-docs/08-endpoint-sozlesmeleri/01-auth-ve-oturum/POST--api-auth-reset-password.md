# POST /api/auth/reset-password

- İşlev alanı: **01 Auth ve oturum**
- İşlev: Tek kullanımlık token ile parolayı değiştirir ve mevcut oturumları iptal eder.
- Operation ID: `POST-/api/auth/reset-password`
- Yetki: **Public / AllowAnonymous** (`security: []`).
- Content-Type: `application/json`.
- Başarı: `204 No Content`, response body yoktur.

## Request body

| Alan | Tip | Zorunlu | Kural |
| --- | --- | --- | --- |
| `token` | string | Evet | E-postadaki URL fragment'ından okunur. |
| `newPassword` | string | Evet | 6–128 karakter ve mevcut parola validator kuralları. |

```json
{
  "token": "raw-token-from-fragment",
  "newPassword": "NewStrongPassword123!"
}
```

## Frontend token kullanımı

E-posta bağlantısı şu biçimdedir:

```text
https://store.example/reset-password#token=...
```

Tarayıcı fragment'ı sunucuya otomatik göndermez. Reset sayfası `window.location.hash` içinden tokenı okuyup yalnız request body içinde API'ye göndermelidir. Tokenı analytics, log, hata izleme bağlamı, localStorage veya query string'e yazmayın. Okuduktan sonra mümkünse `history.replaceState` ile fragment'ı adres çubuğundan temizleyin.

## Güvenlik davranışı

- Token tek kullanımlıktır ve süre sonrasında geçersizdir.
- Geçersiz, kullanılmış ve süresi dolmuş token ayrımı dışarı verilmez; tamamı aynı `401 invalid_or_expired_reset_token` sözleşmesini kullanır.
- Başarılı parola değişiminde kullanıcının `securityVersion` değeri yenilenir ve bütün aktif refresh tokenları iptal edilir. Eski access tokenlar bir sonraki korumalı istekte geçersiz olur.
- Aynı token ile gerçek paralel resetlerde yalnız bir işlem başarılı olur; kaybeden istek tokenı kullanılmış görürse `401`, concurrency yarışına denk gelirse `409` alabilir.
- `forgot-password` ve `reset-password` aynı IP kovasında toplam dakikada 5 istekle sınırlıdır.

## Response sözleşmesi

| HTTP | Gövde | `code` | Açıklama |
| --- | --- | --- | --- |
| `204` | Yok | — | Parola değişti, aktif oturumlar iptal edildi. |
| `400` | `ValidationProblemDetails` | `bad_request` | Token veya yeni parola request doğrulamasından geçmedi. |
| `401` | `ProblemDetails` | `invalid_or_expired_reset_token` | Token geçersiz, kullanılmış veya süresi dolmuş. |
| `409` | `ProblemDetails` | `concurrency_conflict` | Aynı token üzerinde gerçek paralel yazma çakışması. |
| `429` | `ProblemDetails` | `rate_limit_exceeded` | IP limiti aşıldı. |
