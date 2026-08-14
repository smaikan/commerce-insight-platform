# Kimlik Doğrulama ve Kullanıcı API'leri

## Auth endpointleri — Public

### Kayıt

```http
POST /api/auth/register
```

```json
{
  "email": "ali@example.com",
  "password": "StrongPassword123!",
  "firstName": "Ali",
  "lastName": "Yılmaz",
  "phoneNumber": "+905550000000"
}
```

Başarılı response `201 RegisterUserResultDto` döner. Email unique olmalıdır; password server validator'ından geçmelidir.

### Giriş

```http
POST /api/auth/login
```

```json
{ "email": "serhat@serhat.com", "password": "serhat", "deviceName": "Chrome" }
```

Başarılı response `AuthResultDto`:

```json
{
  "user": { "id": "U00001", "email": "ali@example.com", "firstName": "Ali", "lastName": "Yılmaz", "phoneNumber": null, "role": 2, "status": 1, "lastLoginAt": null, "createdAt": "2026-07-27T12:00:00Z", "updatedAt": null },
  "tokens": { "accessToken": "eyJ...", "accessTokenExpiresAt": "2026-07-27T13:00:00Z", "refreshToken": "...", "refreshTokenExpiresAt": "2026-08-26T12:00:00Z" }
}
```

Login veya register sonrasındaki ilk başarılı login cevabından sonra, tarayıcıda `ecommerce_guest_cart` cookie'si varsa access token ile `POST /api/guest-session/claim` çağrılmalıdır. Bu çağrı guest cart ve guest favorites verilerini tek transaction içinde claim eder. Üye tarafındaki alan boşsa guest veri benimsenir; doluysa üye verisi korunur ve guest veri birleştirilmez. Refresh token rotasyonunda claim tekrarlanmaz. Eski istemcilerin `POST /api/cart/merge-guest` çağrısı aynı atomik servisi çalıştırmaya devam eder.

### Token yenileme

```http
POST /api/auth/refresh-token
```

```json
{ "refreshToken": "...", "deviceName": "Chrome" }
```

Refresh token rotation uygulanabileceğinden response'taki yeni token çiftini saklayın.

### Çıkış

```http
POST /api/auth/logout
```

```json
{ "refreshToken": "..." }
```

Başarı `204` döner.

### Şifre unutma / sıfırlama

```http
POST /api/auth/forgot-password
{ "email": "ali@example.com" }

POST /api/auth/reset-password
{ "token": "...", "newPassword": "NewStrongPassword123!" }
```

Forgot-password response her durumda `202` mantığında genel davranır; email var/yok bilgisi UI'ya ifşa edilmemelidir. Reset başarıda `204` döner.

Parola sıfırlama güvenlik sözleşmesi:

- `forgot-password` ve `reset-password` `AllowAnonymous` uçlardır; aynı IP kovasında toplam dakikada 5 istek sınırı uygulanır.
- Var olmayan/pasif hesap ile aktif hesap aynı `202` cevabını alır.
- Aktif token üretildikten sonraki varsayılan 120 saniyede yeni token veya outbox mesajı üretilmez; mevcut bağlantı geçerli kalır.
- E-posta bağlantısı `#token=...` fragment'ı kullanır. Frontend fragment'tan okuduğu tokenı yalnız reset request body içinde gönderir ve URL/log/storage alanlarına kopyalamaz.
- Geçersiz, süresi dolmuş veya kullanılmış token `401` ve `code=invalid_or_expired_reset_token` döndürür.
- Başarılı reset bütün refresh tokenlarını iptal eder ve security version değişimiyle daha önce üretilmiş access tokenları geçersiz kılar.
- Ayrıntılı request/response ve frontend uygulama notları için endpoint sözleşmelerini okuyun: [forgot-password](../08-endpoint-sozlesmeleri/01-auth-ve-oturum/POST--api-auth-forgot-password.md), [reset-password](../08-endpoint-sozlesmeleri/01-auth-ve-oturum/POST--api-auth-reset-password.md).

Production başlangıcında `Email:FromAddress`, `Email:Smtp:Host`, `Email:Smtp:Port`, `Email:Smtp:Username`, `Email:Smtp:Password` ve `Email:PasswordResetUrl` doğrulanır. Reset URL'si production'da HTTPS olmalı ve localhost/loopback hedeflememelidir. SMTP parolasını `appsettings.json` içine yazmayın; örneğin `Email__Smtp__Password` değerini deployment secret store üzerinden sağlayın. Geçersiz yapılandırma API'nin güvenli biçimde başlamamasına neden olur.

## Kullanıcı endpointleri — User

| Method | Endpoint | Amaç | Response |
| --- | --- | --- | --- |
| GET | `/api/users/me` | Aktif kullanıcı | `UserDto` |
| PUT | `/api/users/me/profile` | Profil adı/telefonu | `UserDto` |
| PUT | `/api/users/me/email` | Email değişikliği | `UserDto` |
| PUT | `/api/users/me/password` | Şifre değişikliği | 204 |
| DELETE | `/api/users/me` | Hesabı kapatma | 204 |
| GET | `/api/users/me/sessions` | Oturumlar | `UserSessionDto[]` |
| DELETE | `/api/users/me/sessions/{sessionId}` | Tek oturum iptali | 204 |
| DELETE | `/api/users/me/sessions` | Tüm diğer oturumları kapatma | 204 |

Body alanları ilgili command DTO'larıyla birebir eşleşir. Kullanıcı ID'si route'ta kullanılmaz; `/me` owner scope sağlar.

## Kullanıcı endpointleri — Admin

| Method | Endpoint | Amaç |
| --- | --- | --- |
| GET | `/api/users?pageNumber=1&pageSize=20&search=ali&role=...&status=...` | Kullanıcı listesi |
| GET | `/api/users/{publicUserId}` | `U00001` formatında detay |
| PATCH | `/api/users/{publicUserId}/role` | `{ "role": 1 }` |
| PATCH | `/api/users/{publicUserId}/status` | `{ "status": 1 }` |

Admin role/status değişiklikleri 409 üretebilir; son aktif admin'in kapatılması/rolünün düşürülmesi engellenir.

## JWT ve UI davranışı

- Access token memory/state içinde tutulmalı; refresh token güvenli storage politikasına göre saklanmalıdır.
- 401'de bir kez refresh deneyin; refresh de başarısızsa session temizleyip login'e yönlendirin.
- 403 ile 401'i aynı kullanıcı mesajına dönüştürmeyin: 401 oturum, 403 yetki problemidir.
