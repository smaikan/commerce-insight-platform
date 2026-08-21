# E-Commerce API — Frontend Entegrasyon Dokümanı

Bu dizin ASP.NET Core API’nin storefront ve admin istemcileri için kaynak sözleşmesidir. Wire şeması `openapi-controller-contract.json`, iş akışları bölüm belgeleri, endpoint ayrıntıları `08-endpoint-sozlesmeleri` altındadır. Çelişkide controller/DTO kaynak kodu gerçek runtime davranışı, OpenAPI wire şeması, Markdown ise UI akışı için incelenir ve drift düzeltilir.

## Bölümler

- [Genel API kuralları](00-genel/01-api-kurallari.md)
- [Auth ve kullanıcı](01-auth-kullanici/01-auth.md)
- [Katalog](02-katalog/01-katalog-endpointleri.md)
- [Sepet](03-sepet-siparis/01-sepet.md)
- [Sipariş ve ödeme](03-sepet-siparis/02-siparis-odeme.md)
- [Guest checkout ve erişim](03-sepet-siparis/03-guest-checkout-ve-erisim.md)
- [İade ve değişim](04-iade/01-iade-akisi.md)
- [Adres, kargo, vergi ve kupon](05-yonetim/01-adres-kargo-vergi-kupon.md)
- [Stok](05-yonetim/02-stok.md)
- [Muhasebe](06-muhasebe/01-muhasebe-api.md)
- [Ortak DTO/UI sözleşmeleri](07-ortak/01-dto-ve-ui-sozlesmeleri.md)
- [Endpoint sözleşmeleri](08-endpoint-sozlesmeleri/README.md)
- [İletişim mesajları](08-endpoint-sozlesmeleri/14-iletisim-mesajlari/README.md)

## Checkout seçimi

| Kullanıcı | Endpoint | Sahiplik | Erişim sonrası |
| --- | --- | --- | --- |
| Üye | `POST /api/orders` | `Order.UserId=<JWT user>` | JWT ile `/api/orders/*` |
| Misafir | `POST /api/cart/checkout/guest` | `Order.UserId=null` | 7 günlük guest session veya 30 dakikalık tek kullanımlık magic link |

Ortak/default guest User oluşturulmaz. Frontend hiçbir checkout türünde `UserId`, fiyat, vergi, indirim, kargo ücreti, stok veya toplam göndermez. İki akış da aynı backend checkout orkestratörünü, aktif kargo kaydını, kupon/vergi hesabını, stok rezervasyonunu ve `StockMovement` mekanizmasını kullanır.

## Frontend başlangıç sırası

1. Public katalog ve aktif kargo yöntemlerini okuyun.
2. Cart’ın son `concurrencyToken` değerini her mutasyondan sonra değiştirin.
3. Üye checkout’ta JWT; guest checkout’ta same-origin BFF, guest cookie, `Origin`, `Idempotency-Key` ve gerektiğinde `X-Turnstile-Token` kullanın.
4. `409` durumunda kaynağı yeniden okuyun; aynı checkout intent’i yeniden deneniyorsa idempotency key’i değiştirmeyin.
5. Sipariş/cart/guest cevaplarını `no-store` tutun. Token/cookie/PII değerlerini DOM, localStorage, log veya analytics’e açmayın.

## Yetki kısaltmaları

| Kısaltma | Anlam |
| --- | --- |
| Public | JWT gerekmez |
| Guest session | Secure/HttpOnly guest cookie ve route’a göre CSRF/Origin gerekir |
| User | Geçerli JWT ve owner kontrolü gerekir |
| Admin | JWT + `AdminOnly` policy gerekir |
