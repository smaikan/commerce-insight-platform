# POST /api/cart/checkout/guest

Mevcut anonymous cart’ı User hesabı oluşturmadan Order’a çevirir. JWT ile çağrılırsa üye checkout endpointi kullanılmalıdır.

## Yetki, cookie ve header

- Authorization: JWT yok.
- Cookie: `ecommerce_guest_cart` (yoksa API üretir); mevcutsa `ecommerce_guest_orders` reuse edilir.
- `Origin`: zorunlu ve `GuestProtection:TrustedOrigins` allowlist’inde.
- `Idempotency-Key`: zorunlu, en fazla 200 karakter; aynı intent retry’ında korunur.
- `X-Turnstile-Token`: 10 dakikadaki üçüncü denemeden itibaren veya Redis fallback’te zorunlu.

## Body

| Alan | Tip | Required | Nullable | Not |
| --- | --- | --- | --- | --- |
| expectedCartConcurrencyToken | uuid | Evet | Hayır | Son CartDto tokenı |
| customer.firstName | string(100) | Evet | Hayır | Snapshot |
| customer.lastName | string(100) | Evet | Hayır | Snapshot |
| customer.email | email(320) | Evet | Hayır | Normalize edilir |
| customer.phoneNumber | string(30) | Evet | Hayır | Snapshot |
| shippingAddress | object | Evet | Hayır | SourceAddressId kabul edilmez |
| shippingAddress.title/firstName/lastName/phoneNumber/city/district/neighborhood/fullAddress | string | Evet | Hayır | Uzunluklar Address sözleşmesi |
| shippingAddress.postalCode | string(20) | Hayır | Evet |  |
| billingAddress | aynı object | Hayır | Evet | null ise shipping → Billing fallback |
| shippingMethodId | uuid | Evet | Hayır | Aktif kayıt zorunlu |
| couponCode | string(50) | Hayır | Evet | Üye/guest uygunluğu backend’de |

```http
POST /api/cart/checkout/guest
Origin: https://store.example.com
Idempotency-Key: 01J4GUEST8R6F3K
Content-Type: application/json
```

```json
{
  "expectedCartConcurrencyToken": "77a50a8f-8dc1-4aa7-aed0-71ac4e58a8bb",
  "customer": {"firstName":"Ada","lastName":"Lovelace","email":"ada@example.com","phoneNumber":"+905551112233"},
  "shippingAddress": {"title":"Ev","firstName":"Ada","lastName":"Lovelace","phoneNumber":"+905551112233","city":"İstanbul","district":"Kadıköy","fullAddress":"Örnek Sokak 1","postalCode":"34000"},
  "billingAddress": null,
  "shippingMethodId": "893fdb48-e9cf-4c8c-a94c-3b989697f204",
  "couponCode": "WELCOME10"
}
```

## Başarı

Yeni işlem `201`, aynı key/body replay `200`; body `OrderDto`:

```json
{
  "id": "3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26",
  "orderNumber": "ORD-6F7951F775D04EF5B7E8",
  "status": 0,
  "subTotal": 1000.00,
  "discountTotal": 100.00,
  "shippingTotal": 49.90,
  "taxTotal": 180.00,
  "grandTotal": 1129.90,
  "couponCode": "WELCOME10",
  "shippingMethodName": "Standart Kargo",
  "customer": {"firstName":"Ada","lastName":"Lovelace","email":"ada@example.com","phoneNumber":"+905551112233"},
  "shippingAddress": {"sourceAddressId":null,"title":"Ev","firstName":"Ada","lastName":"Lovelace","phoneNumber":"+905551112233","city":"İstanbul","district":"Kadıköy","fullAddress":"Örnek Sokak 1","postalCode":"34000"},
  "billingAddress": {"sourceAddressId":null,"title":"Ev","firstName":"Ada","lastName":"Lovelace","phoneNumber":"+905551112233","city":"İstanbul","district":"Kadıköy","fullAddress":"Örnek Sokak 1","postalCode":"34000"},
  "items": [{
    "productTitle": "Ürün",
    "variantSku": "SKU-PUDRA",
    "variantName": "Renk",
    "variantValue": "Pudra"
  }],
  "payments": [], "reservationExpiresAt": "2026-08-03T12:15:00Z", "createdAt": "2026-08-03T12:00:00Z"
}
```

`variantName` ve `variantValue` checkout anında sipariş kalemine snapshot'lanır; varyantsız üründe veya eski siparişte `null` olabilir. Ayrıntı: [varyant snapshot sözleşmesi](SEPET-SIPARIS-VARYANT-SNAPSHOT-SOZLESMESI.md).

İlk başarı `ecommerce_guest_orders` ve `ecommerce_guest_csrf` cookie’lerini 7 gün, Secure/HttpOnly/SameSite=Lax/Path=/api olarak set eder. Cart transaction’da temizlenir. Kargo adı/ücreti, vergi, indirim, toplam ve stock-out backend kaynaklıdır. Order/item/customer/address/shipping snapshot, CouponUsage, tek Sale StockMovement/varyant, rezervasyon, grant, idempotency ve outbox aynı transaction’dadır. SMTP beklenmez.

## Hatalar ve recovery

| HTTP | code | Sebep / frontend |
| --- | --- | --- |
| 400 | validation_error/bad_request | Required/body/header; alan hatalarını göster |
| 401 | unauthorized | Bu endpointte normal değil; JWT gönderildiyse üye endpointine geç |
| 403 | invalid_guest_access | Origin güvenilir değil; retry öncesi BFF config düzelt |
| 404 | resource_not_found | Cart veya shipping method yok; cart/kargo listesini yenile |
| 409 | concurrency_conflict | Cart’ı GET et, kullanıcıya değişikliği göster |
| 409 | coupon_members_only | “Bu kupon yalnızca üyeler içindir”; otomatik retry yok |
| 409 | idempotency_key_reused/conflict | Aynı key farklı body veya stok/kargo/kupon conflict |
| 428 | guest_checkout_challenge_required | Turnstile göster; aynı body/key ile tekrar |
| 429 | guest_checkout_rate_limited | Bekle; yeni key ile bypass etme |
| 500 | internal_error | Draft ve key’i koru; traceId göster |
| 503 | guest_checkout_protection_unavailable | Geçici koruma hatası; bypass etme |

PII, cookie, raw Turnstile tokenı ve Idempotency-Key log/analytics’e yazılmaz. Loading sırasında tek intent bir kez disable edilir; timeout sonrası aynı key korunur.


