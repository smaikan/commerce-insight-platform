# DELETE /api/product-engagement/products/{productId}/favorites

- Görev alanı: **Katalog → Ürün etkileşimleri → Favoriler**.
- İşlev: Favorilerden çıkarır.
- Operation ID: `DELETE-/api/product-engagement/products/{productId}/favorites`
- Yetki: **User / guest session**.
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

Favori ilişkisi kaldırıldığında `204 No Content` döner. Ürün veya owner'a ait favori ilişkisi bulunamazsa `404` döner.

JWT isteğinde guest cookie dikkate alınmaz. Guest isteği ortak `ecommerce_guest_cart` cookie'sini, trusted `Origin` değerini ve cookie tokenıyla eşleşen `X-Guest-CSRF` header'ını birlikte taşır. Eksik session `401`; Origin veya CSRF doğrulama hatası `403` döndürür. Favori kaydı, `Product.favoriteCount` ve popularity score aynı serializable transaction içinde güncellenir; geçmiş günlük ekleme metriği silme sırasında azaltılmaz.

Başarı/hata kodları: `204`, `400`, `401`, `403`, `404`.

