# POST /api/auth/forgot-password

- İşlev alanı: **01 Auth ve oturum**
- İşlev: Kullanıcı varlığını açığa çıkarmadan parola sıfırlama e-posta akışını başlatır.
- Operation ID: `POST-/api/auth/forgot-password`
- Yetki: **Public / AllowAnonymous** (`security: []`).
- Content-Type: `application/json`.
- Başarı: `202 Accepted`, response body yoktur.

## Request body

| Alan | Tip | Zorunlu | Kural |
| --- | --- | --- | --- |
| `email` | string | Evet | Geçerli e-posta, en fazla 320 karakter. |

```json
{
  "email": "user@example.com"
}
```

## Güvenlik ve tekrar davranışı

- Aktif kullanıcı bulunsa da bulunmasa da aynı `202` ve boş gövde döner. Frontend kullanıcı varlığını bu endpoint üzerinden çıkarmamalıdır.
- Token ve e-posta outbox kaydı aynı transaction içinde oluşturulur; HTTP isteği SMTP gönderimini beklemez.
- Varsayılan `120` saniyelik e-posta cooldown süresinde aktif token korunur. Yeni token ve ikinci outbox mesajı üretilmez; endpoint yine `202` döner.
- Cooldown `Auth:PasswordResetRequestCooldownSeconds` deployment ayarıyla değiştirilebilir.
- `forgot-password` ve `reset-password` aynı IP kovasında toplam dakikada 5 istekle sınırlıdır.
- Ham token veritabanında tutulmaz. Outbox içinde Data Protection ile korunur ve yalnız gönderim sırasında çözülür.
- E-posta bağlantısındaki token query string'de değil `#token=...` URL fragment'ında taşınır.

## Response sözleşmesi

| HTTP | Gövde | `code` | Açıklama |
| --- | --- | --- | --- |
| `202` | Yok | — | İstek kullanıcı varlığı açıklanmadan kabul edildi. |
| `400` | `ValidationProblemDetails` | `bad_request` | E-posta biçimi veya uzunluğu geçersiz. |
| `429` | `ProblemDetails` | `rate_limit_exceeded` | IP limiti aşıldı. |

Frontend `202` sonrasında her durumda “E-posta kayıtlıysa sıfırlama bağlantısı gönderildi” gibi genel bir mesaj göstermelidir.
