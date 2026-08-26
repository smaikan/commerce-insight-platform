# Hızlı Başlangıç

Bu sayfa, API'ye ilk başarılı isteği göndermek için gereken en kısa yolu gösterir.

## 1. Base URL

Base URL ortama göre değişir. İstemci kodunda production alan adı sabit yazılmamalıdır.

```text
https://api.example.com
```

Bütün iş endpointleri `/api` ile başlar ve JSON kullanır.

## 2. Public katalog isteği

Public endpointlerde Bearer token gerekmez:

```bash
curl --request GET \
  --url 'https://api.example.com/api/products/published?PageNumber=1&PageSize=24&SortBy=0&Descending=true' \
  --header 'Accept: application/json'
```

Sayfalı bir yanıtın genel biçimi şöyledir:

```json
{
  "items": [],
  "pageNumber": 1,
  "pageSize": 24,
  "totalCount": 0,
  "totalPages": 0,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

## 3. Oturum açma

```bash
curl --request POST \
  --url 'https://api.example.com/api/auth/login' \
  --header 'Content-Type: application/json' \
  --data '{
    "email": "kullanici@example.com",
    "password": "GuvenliParola123!",
    "deviceName": "Web"
  }'
```

Başarılı yanıt kullanıcıyı ve iki tokenı döndürür:

```json
{
  "user": {
    "id": "U00001",
    "email": "kullanici@example.com",
    "firstName": "Deniz",
    "lastName": "Yılmaz",
    "role": 1,
    "status": 1
  },
  "tokens": {
    "accessToken": "<access-token>",
    "accessTokenExpiresAt": "2026-08-26T12:15:00Z",
    "refreshToken": "<refresh-token>",
    "refreshTokenExpiresAt": "2026-09-25T12:00:00Z"
  }
}
```

Tokenları loglara, URL'ye veya public örneklere yazmayın. Browser uygulamalarında token yönetimi için HttpOnly cookie kullanan güvenli bir BFF sınırı tercih edilmelidir.

## 4. Yetkili istek

```bash
curl --request GET \
  --url 'https://api.example.com/api/users/me' \
  --header 'Accept: application/json' \
  --header 'Authorization: Bearer <access-token>'
```

Admin endpointleri de aynı header'ı kullanır; ayrıca token içindeki rolün `Admin` olması gerekir.

## 5. Hata yanıtını ele alma

API hataları çoğunlukla `application/problem+json` biçimindedir:

```json
{
  "type": "urn:ecommerce:error:validation_error",
  "title": "Validation failed",
  "status": 400,
  "detail": "İstek alanlarından biri geçersiz.",
  "instance": "/api/orders",
  "code": "validation_error",
  "traceId": "00-redacted",
  "timestamp": "2026-08-26T12:00:00Z",
  "errors": {
    "shippingMethodId": [
      "Aktif bir kargo yöntemi seçilmelidir."
    ]
  }
}
```

İstemci kararını yalnız hata metnine göre değil, öncelikle HTTP status ve kararlı `code` alanına göre vermelidir.

## Sonraki adım

- Token ve rol kuralları: [Kimlik doğrulama ve yetkilendirme](02-kimlik-dogrulama-ve-yetkilendirme.md)
- Sayfalama, tarih ve kimlik biçimleri: [Ortak kurallar](03-ortak-kurallar.md)
- Retry, idempotency ve concurrency: [Hatalar ve güvenli tekrar](04-hatalar-ve-guvenli-tekrar.md)
- Tüm operasyonlar: [Endpoint referansı](../03-endpoint-referansi/README.md)

