# Ortak DTO, Enum ve UI Sözleşmeleri

## Guest checkout DTO ve UI sözleşmesi — 3 Ağustos 2026

`OrderDto` değişmez `customer`, `shippingAddress` ve `billingAddress` snapshot’larını döndürür. `OrderCustomerDto={firstName,lastName,email,phoneNumber}`. `OrderAddressDto.sourceAddressId` nullable’dır; guest adreslerinde null, kayıtlı üye adresinden alınan snapshot’ta kaynak GUID’dir. Shipping name/fee, item fiyatı, discount/tax/grand total otoriter backend değerleridir.

`CouponDto`, create ve update body’sinde `isMemberOnly:boolean` required/non-nullable, varsayılan false’dur. Guest için true kupon `409 coupon_members_only` üretir.

| Guest checkout alanı | Required | Nullable |
| --- | --- | --- |
| expectedCartConcurrencyToken | Evet | Hayır |
| customer.firstName/lastName/email/phoneNumber | Evet | Hayır |
| shippingAddress ad/telefon/şehir/ilçe/tam adres | Evet | Hayır |
| shippingAddress.postalCode | Hayır | Evet |
| billingAddress | Hayır | Evet; null ise shipping fallback |
| shippingMethodId | Evet | Hayır |
| couponCode | Hayır | Evet |

Body’de `UserId`, address source ID, fiyat, vergi, indirim, kargo ücreti, stok, toplam veya Order status yoktur. Guest cookie/token response modeline çevrilmez; BFF `Set-Cookie` taşır. Magic exchange yalnız `orderId` ve `sessionExpiresAt` döndürür. Guest/cart/order cevapları no-store’dur.

| code | UI davranışı |
| --- | --- |
| coupon_members_only | Mesajı göster, login/kupon kaldır sun; otomatik retry yok |
| idempotency_key_reused | Aynı key farklı intent; draftı koru ve kullanıcı kararı iste |
| guest_checkout_challenge_required | Turnstile göster; başarılı olunca aynı body/key |
| guest_checkout_rate_limited | Kontrollü bekle; hızlı otomatik retry yok |
| guest_checkout_protection_unavailable | Geçici hata; korumayı bypass etme |
| invalid_guest_access | Magic-link recovery; cross-order ayrıntısı gösterme |

## Lifecycle aksiyon matrisi

| Kaynak | Draft | Posted/Paid | Cancelled/Failed |
| --- | --- | --- | --- |
| Product | düzenle/status/activation/relations | status/activation | archived/passive göster |
| Cart | add/update/remove/clear | checkout'a gönder | token yenile |
| Order | ödeme/uygun cancel | user action'a göre; Admin status | detail read-only |
| ReturnRequest | customer create | Admin approve/reject/receive/complete | terminal read-only |
| AccountingSalesOrder | header/items düzenle, post | cancel ile reversal | yeniden oluştur |
| SalesInvoice | header/tam lines düzenle, post | cancel; fiziksel stok yok | yeniden oluştur |
| PurchaseInvoice | header/lines/allocation/expense düzenle, post | cancel policy | yeniden oluştur |
| Payment | oluştur | cancel/reversal | read-only |

İptal/reversal sonrası orijinal kayıt silinmez. UI detail/list endpointini tekrar okumalı; status, cancelledAt, cancellationReason, reversal linkleri, paid/remaining ve balance alanlarını güncellemelidir.

## Ortak DTO biçimleri

### PagedResult

Alanlar: items, pageNumber, pageSize, totalCount, totalPages, hasPreviousPage, hasNextPage.

### Public Product/User ID

UserDto alanları: id (U prefix), email, firstName, lastName, phoneNumber, role, status, lastLoginAt, createdAt, updatedAt.

Admin kullanıcı listesi `AdminUserDto` ayrıca kullanıcının üyelikli siparişlerinin toplamını `orderCount` alanında döner.

ProductDto alanları: id (P prefix), title, mainSku, description, url, typeId/typeName, brandId/brandName, taxRateId/taxRateName/taxRatePercentage, status, isActive, isFeatured, `hasVariants`, displayOrder, seoTitle, seoDescription, analytics counters, variants ve tags. `hasVariants` kalıcı request/response alanıdır, varsayılanı `false`tur ve varyant sayısı birden fazlaysa `true` olmalıdır.

### Adres

AddressDto alanları: id, type, title, firstName, lastName, phoneNumber, city, district, neighborhood, fullAddress, postalCode, isDefault.

## Cari ve finans işaretleme

- CustomerReceivable: debit (+), tahsilat credit (-).
- SupplierDebt: credit (+), tediye debit (-).
- Rapor satırında amount debit, secondaryAmount credit, tertiaryAmount valid remaining anlamındadır.
- Payment amount brüt ödeme, allocatedAmount geçerli tahsis, unallocatedAmount avans/tahsissiz bölümdür.
- Kasa/banka statement balanceAfter ledger'dan gelir; frontend balance yazamaz.

## Hata ve frontend davranışı

| code | UI davranışı |
| --- | --- |
| validation_error | errors[field] alanına göster |
| business_rule_violation | detail mesajını göster; formu koru |
| resource_not_found | Detaydan çık/listeyi yenile |
| conflict | Duplicate veya iş kuralı; mevcut kaydı silme |
| concurrency_conflict | Güncel response'u yeniden al; overwrite seçeneği sunma |
| forbidden | Yetki kontrolü; admin aksiyonunu gizle |

## Benzersizlik ve concurrency

- Product MainSku, brand/collection/tag URL, shipping/tax/category code, kasa/banka code ve invoice/order number çakışmaları çoğunlukla 409'dur.
- Sipariş/cart concurrency token response'tan alınır ve sonraki mutation'da geri gönderilir.
- 409 sonrasında POST/PUT'u otomatik tekrar etmeyin; önce GET ile güncel state'i okuyun.
- Idempotency kullanılan endpointlerde aynı key'i koruyarak retry güvenlidir; farklı key duplicate effect yaratabilir veya reddedilebilir.

## Enum merkezi referansı

| Enum | Değerler |
| --- | --- |
| ProductStatus | Draft, Active, Passive, Archived |
| OrderStatus | Pending, Confirmed, Paid, Preparing, Shipped, Delivered, Cancelled, Refunded, ReturnRequested, ReturnApproved |
| PaymentStatus | Pending, Paid, Failed, Refunded, Cancelled |
| PaymentProvider | Fake, Iyzico, Stripe, PayTR |
| CouponDiscountType | Percentage, FixedAmount |
| ReturnType | Refund, Exchange |
| StockMovementDirection | In=1, Out=2 |
| AddressType | Shipping, Billing |

Accounting enum numeric değerleri muhasebe bölümünde verilmiştir; Accounting enumlarını string göndermeyin.

## Liste ve boş sonuç

- Query sayfalı ise pageNumber=1 ve pageSize=20 varsayılandır.
- items=[] başarılı boş sonuçtur; 404 değildir.
- Public listelerde yalnız aktif/visible kayıtlar dönmüş olabilir.
- Admin listelerinde filtreler yalnız endpoint imzasında varsa kullanılabilir.
- Product listesinde sortBy/descending vardır; diğer listelerde backend'in deterministik sırasını kullanın.


