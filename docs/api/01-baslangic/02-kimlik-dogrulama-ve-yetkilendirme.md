# Kimlik Doğrulama ve Yetkilendirme

API, JWT access token ve dönen refresh token kullanan oturum modeli uygular. Bir endpointin token kabul etmesi, kullanıcının o kaynağa erişebileceği anlamına gelmez; rol ve kaynak sahipliği ayrıca kontrol edilir.

## Erişim seviyeleri

### Public

Bearer token zorunlu değildir. Public katalog, auth, mağaza ayarlarının public görünümü, iletişim formu ve bazı provider callbackleri bu sınıftadır.

Public demek korumasız demek değildir. Endpoint şu kontrollerden birini uygulayabilir:

- IP veya kimlik temelli rate limit
- Trusted `Origin` doğrulaması
- Cloudflare Turnstile challenge
- İmzalı provider callback/webhook
- Güvenli guest cookie

### Guest session

Misafir sepeti ve sipariş erişiminde JWT yerine API'nin ürettiği HttpOnly cookie kullanılır. Cookie değerini JavaScript'e açmayın.

Mutasyonlarda endpoint sözleşmesine göre şu ek bilgiler istenebilir:

- `Origin`
- `X-Guest-CSRF`
- `Idempotency-Key`
- `X-Turnstile-Token`

Guest siparişlerde farklı bir siparişin varlığı veya sahibi açıklanmaz; geçersiz erişim genellikle aynı güvenli `404` sözleşmesine indirgenir.

### User

İstek şu header'ı taşır:

```http
Authorization: Bearer <access-token>
```

`User` endpointleri oturum açmış müşteriye aittir. Profil, adres, sepet devri, kendi siparişleri, ödeme, iptal ve iade taleplerinde owner kontrolü uygulanır. Başka müşteriye ait kaynak için API çoğunlukla güvenli `404` döndürür.

### Admin

Admin endpointleri Bearer token yanında `AdminOnly` policy uygular. Tokenın rol claim'i `Admin` olmalıdır.

```text
UserRole.Customer = 1
UserRole.Admin    = 2
```

Admin erişimi katalog yönetimi, tüm siparişler, iade operasyonları, kullanıcı yönetimi, stok, kupon, dashboard, mağaza ayarları, iletişim gelen kutusu ve muhasebe alanlarında kullanılır.

### Provider

iyzico callback/webhook endpointleri kullanıcı Bearer tokenıyla değil, sağlayıcıdan gelen token ve imza doğrulamasıyla çalışır. Browser yönlendirmesi tek başına ödemenin başarılı sayılması için yeterli değildir.

## Access ve refresh token

- Access token kısa ömürlü API erişimidir.
- Refresh token yeni token çifti üretmek için kullanılır ve her başarılı yenilemede rotate edilir.
- Eski refresh token yeniden kullanılmamalıdır.
- İstemci bir `401` sonrasında refresh'i en fazla bir kez deneyip asıl isteği bir kez tekrarlamalıdır.
- Refresh de başarısızsa yerel oturum temizlenmelidir.
- `403`, refresh ile düzeltilecek bir kimlik doğrulama hatası değildir; kullanıcının yetkisi yoktur.

## Auth endpointleri

| Endpoint | Yetki | Sonuç |
| --- | --- | --- |
| `POST /api/auth/register` | Public | Hesap oluşturur. |
| `POST /api/auth/login` | Public | Kullanıcı ve token çiftini döndürür. |
| `POST /api/auth/refresh-token` | Public | Refresh tokenı rotate eder. |
| `POST /api/auth/logout` | Public | Gönderilen refresh oturumunu kapatır. |
| `POST /api/auth/forgot-password` | Public | Hesap varlığını açıklamadan reset akışını başlatır. |
| `POST /api/auth/reset-password` | Public | Geçerli reset tokenıyla parolayı değiştirir. |

Ayrıntılı body ve response örnekleri için [Auth endpointleri](../03-endpoint-referansi/01-kimlik-ve-kullanicilar/kimlik-dogrulama/README.md) bölümünü kullanın.

## Güvenlik notları

- Access veya refresh tokenı URL query parametresine koymayın.
- Token, parola, ödeme verisi ve guest cookie değerini loglamayın.
- Browser uygulamasında tokenları `localStorage` veya browser tarafından okunabilen cookie'de saklamayın.
- `401` ile `403` durumlarını aynı kabul etmeyin.
- Owner-scoped `404` yanıtını kullanıcı veya kaynak keşfi için kullanmaya çalışmayın.
- Admin rolü istemci tarafında gizlenen butonla değil, API policy'siyle korunur.

