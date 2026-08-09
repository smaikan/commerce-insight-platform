# Adres, Kargo, Vergi ve Kupon Yönetimi

## Adres

Üye Address endpointleri değişmez; AddressType 0 Shipping, 1 Billing’dir. Checkout’ta seçili shipping adresi OrderAddressSnapshot olur. Guest adresi Address tablosuna eklenmez ve `SourceAddressId=null` snapshot olarak tutulur. Guest billing gönderilmezse shipping alanları Billing snapshot’a kopyalanır.

## Kargo

Storefront aktif seçenekleri `GET /api/shipping-methods/active?pageNumber=1&pageSize=100` ile okur. Üye ve guest checkout’ta `shippingMethodId` zorunludur. API transaction sırasında yöntemi tekrar takipli okur; bulunamazsa 404, pasifse 409 ve sipariş oluşmaz. `name` ve `fixedFee` backend’den snapshot edilir. Frontend kargo ücreti veya adı gönderemez.

Admin endpointleri: `GET/POST /api/shipping-methods`, `GET/PUT /api/shipping-methods/{id}`, `PATCH /api/shipping-methods/{id}/activation`.

## Vergi

Aktif oranlar `GET /api/tax-rates/active`; yönetim CRUD/activation Admin’dir. Product TaxRate ilişkisi backend’de Variant net fiyat, indirim sonrası taxable amount ve Order tax snapshot hesabında kullanılır. Frontend vergi oranı veya tutarı checkout’a göndermez.

## Kupon ve isMemberOnly

Coupon DTO/create/update sözleşmesine `isMemberOnly: boolean` eklenmiştir; required, nullable değildir ve varsayılan `false`’tur.

```json
{
  "code": "WELCOME10",
  "discountType": 0,
  "discountValue": 10,
  "description": "Hoş geldin",
  "minimumOrderAmount": 100,
  "usageLimit": 1000,
  "startsAt": "2026-08-01T00:00:00Z",
  "expiresAt": "2026-09-01T00:00:00Z",
  "isActive": true,
  "isMemberOnly": false
}
```

- `false`: guest ve üye checkout kullanabilir.
- `true`: guest, indirim hesabından önce `409 coupon_members_only` alır; üye normal uygunluk kurallarına devam eder.
- Guest kullanım `CouponUsage.UserId=null`, `OrderId` dolu kaydedilir.
- Sipariş iptalinde sayaç/kullanım mevcut release akışıyla geri alınır.
- Guest claim’de CouponUsage UserId yeni hesap sahibine bağlanır.

Admin listesi/formu `isMemberOnly` kolon/kontrolünü göstermeli; false değeri “tüm müşteriler”, true “yalnız üyeler” anlamına gelir. UI bu alanı kampanya türü veya indirim hesabı gibi yorumlamaz.
