# POST /api/product-engagement/products/{productId}/favorites

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Yeni kaynak veya iş akışı adımı oluşturur/başlatır.
- Operation ID: `POST-/api/product-engagement/products/{productId}/favorites`
- Yetki: `AllowAnonymous`; JWT veya doğrulanmış guest session kullanılabilir.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `productId` | path | Evet | string |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (204)

Response body yoktur.

İlk ekleme `204 No Content` döndürür. Aynı owner aynı ürünü tekrar eklerse standart ProblemDetails ile `409 Conflict` döner. Ürün bulunamazsa `404` döner.

JWT isteğinde guest header/cookie dikkate alınmaz. Guest isteğinde önce `GET /api/product-engagement/favorites` ile ortak HttpOnly session cookie'si kurulmuş olmalıdır. Browser cookie değerini JavaScript'e açmaz; same-origin BFF cookie'yi sunucu tarafında okuyup upstream isteğine hem cookie hem `X-Guest-CSRF` header olarak taşır. `Origin`, `GuestProtection:TrustedOrigins` listesinden olmalıdır. Eksik session `401`; güvenilmeyen Origin veya uyuşmayan CSRF header `403` döndürür. Ham session token loglanmamalı veya istemci durumuna yazılmamalıdır.

Favori kaydı, `Product.favoriteCount`, popularity score ve günlük favori metriği aynı serializable transaction içinde güncellenir.

Başarı/hata kodları: `204`, `400`, `401`, `403`, `404`, `409`.

