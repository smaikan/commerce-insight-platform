# Hatalar, Idempotency ve Güvenli Tekrar

## ProblemDetails

API, doğrulama ve iş kuralı hatalarını kararlı bir ProblemDetails gövdesiyle döndürür:

```json
{
  "type": "urn:ecommerce:error:concurrency_conflict",
  "title": "Concurrency conflict",
  "status": 409,
  "detail": "Kayıt bu sırada değişti. Güncel veriyi okuyup tekrar deneyin.",
  "instance": "/api/cart/items/78f51ad7-16eb-4c5f-b4e3-a4db481a01d2",
  "code": "concurrency_conflict",
  "traceId": "00-redacted",
  "timestamp": "2026-08-26T12:00:00Z"
}
```

Validation hatalarında ek `errors` alanı bulunabilir:

```json
{
  "status": 400,
  "code": "validation_error",
  "errors": {
    "email": [
      "Geçerli bir e-posta adresi girilmelidir."
    ]
  }
}
```

## HTTP durumları

| HTTP | Anlam | İstemci davranışı |
| --- | --- | --- |
| `400` | Request biçimi veya alan doğrulaması geçersiz | Alan hatalarını göster; aynı body'yi kör tekrar etme |
| `401` | Token yok, geçersiz veya süresi dolmuş | Uygunsa bir kez refresh et; yine başarısızsa oturumu kapat |
| `403` | Kimlik doğrulandı ancak yetki/Origin/CSRF reddedildi | Refresh yapma; kullanıcıya uygun erişim hatası göster |
| `404` | Kaynak yok veya owner-scoped erişim gizlendi | Kaynağın başka kullanıcıya ait olduğunu varsayma |
| `409` | İş kuralı, durum veya concurrency çatışması | Güncel kaynağı yeniden oku; `code` değerine göre karar ver |
| `413` | Body sınırı aşıldı | Payloadı küçült |
| `428` | Ek doğrulama gerekli | Turnstile gibi istenen doğrulamayı tamamla |
| `429` | Rate limit | `Retry-After` süresine uy; hızlı otomatik retry yapma |
| `500` | Beklenmeyen sunucu hatası | Trace ID'yi destek kaydında kullan; hassas veri ekleme |
| `503` | Geçici bağımlılık/koruma servisi sorunu | Formu koru, kontrollü daha sonra dene; güvenlik bypass etme |

## Concurrency token

Sepet ve bazı admin mutasyonları güncel `concurrencyToken` ister. Her başarılı mutasyon sonrası response'taki yeni token saklanmalıdır.

`409 concurrency_conflict` alındığında:

1. Kaynağı yeniden GET edin.
2. Kullanıcıya güncel durumu gösterin.
3. Eski değişikliği otomatik overwrite etmeyin.
4. Kullanıcı hâlâ aynı işlemi istiyorsa yeni tokenla yeni bir intent oluşturun.

## Idempotency-Key

Checkout, ödeme ve bazı mesaj operasyonları `Idempotency-Key` ister.

```http
Idempotency-Key: 8e969006-ea58-4bdf-9e6f-8aec64bb21f4
```

Kurallar:

- Aynı kullanıcı intent'inin timeout/network retry'ında aynı key ve aynı body kullanılır.
- Kullanıcı body'yi veya seçimini değiştirdiyse yeni intent ve yeni key oluşturulur.
- Aynı key farklı body ile gönderilirse `409 idempotency_key_reused` beklenir.
- Non-idempotent bir mutasyon endpoint sözleşmesi izin vermiyorsa otomatik tekrar edilmez.

## Ödeme sonucu belirsizliği

Provider timeoutu kesin başarısızlık değildir; tahsilat yapılmış olabilir. API böyle bir durumda siparişi veya stoğu hemen değiştirmek yerine mutabakat başlatabilir ve `202 Accepted` döndürebilir. İstemci response'taki polling URL'ini kontrollü aralıklarla okumalıdır.

## Rate limit

`429` yanıtında `Retry-After` header'ı varsa saniye cinsinden bekleme süresidir. Login, cart, order, payment, guest checkout ve contact endpointleri farklı limit politikalarına sahip olabilir.

