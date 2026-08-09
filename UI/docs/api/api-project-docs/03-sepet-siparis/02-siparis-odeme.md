# Sipariş ve Ödeme API’leri

E-ticaret `Order`, AccountingSalesOrder’dan ayrıdır. Üye ve guest checkout aynı `OrderCheckoutOrchestrator` kullanır; yalnız sahiplik ve erişim kanalı değişir.

## Üye checkout

```http
POST /api/orders
Authorization: Bearer <access-token>
Content-Type: application/json
```

```json
{
  "expectedCartConcurrencyToken": "77a50a8f-...",
  "shippingAddressId": "5ec7c37b-...",
  "shippingMethodId": "893fdb48-...",
  "couponCode": "WELCOME10"
}
```

| Alan | Required | Nullable | Kaynak |
| --- | --- | --- | --- |
| expectedCartConcurrencyToken | Evet | Hayır | Son CartDto |
| shippingAddressId | Evet | Hayır | JWT kullanıcısına ait Shipping Address |
| shippingMethodId | Evet | Hayır | Aktif shipping method |
| couponCode | Hayır | Evet | Kullanıcı girdisi |

Kargo yöntemi ve teslimat adresi zorunludur. Billing snapshot teslimat adresinden üretilir. Müşteri snapshot User’dan; telefon User’da yoksa shipping address’ten alınır.

## Guest checkout

Tam sözleşme: [Guest checkout ve erişim](03-guest-checkout-ve-erisim.md) ve [endpoint belgesi](../08-endpoint-sozlesmeleri/04-sepet/POST--api-cart-checkout-guest.md). Guest müşteri, shipping, aktif `shippingMethodId`, cart token ve `Idempotency-Key` zorunludur; billing opsiyonel ve yoksa shipping’den kopyalanır.

## Backend otoritesi ve transaction

Her iki checkout akışında backend:

1. cart ve concurrency tokenı transaction kilidi altında doğrular;
2. ProductVariant/Product/TaxRate kayıtlarını yeniden okur;
3. aktif shipping method adını ve `FixedFee` değerini backend’den alır;
4. kupon aktiflik, tarih, limit, minimum ve guest/member uygunluğunu denetler;
5. indirim, vergi, shipping ve grand totalı hesaplar;
6. Order, item, customer, shipping, billing ve shipping-method snapshot’larını oluşturur;
7. kupon kullanımını `OrderId` ile kaydeder;
8. her varyant için yalnız mevcut `StockMovementType.Sale` negatif hareketini üretir;
9. 15 dakikalık ödeme rezervasyonu ve satın alma metriğini kaydeder;
10. cart’ı temizler ve e-posta outbox kaydını ekler.

Guest’te session/grant, idempotency ve protected magic-link outbox kayıtları da aynı transaction’dadır. SMTP gönderimi checkout response’unu bekletmez. Stok doğrudan kolon azaltılarak veya ikinci guest yolu açılarak değiştirilmez.

## OrderDto

OrderDto `customer`, `shippingAddress`, `billingAddress`, `shippingMethodName`, `shippingTotal`, `items`, `payments`, `reservationExpiresAt`, `status` ve bütün backend toplamlarını döndürür. Guest adreslerinde `sourceAddressId=null` olur. Üye siparişinde `UserId` response’a açılmaz; guest siparişte veritabanı sahipliği null’dır.

Sıfır toplamlı tam kupon siparişi payment oluşturmadan `Paid` olabilir ve stok rezervasyon süresi taşımaz. Pozitif toplamlı sipariş 15 dakika rezervasyon alır.

## Yönetim sipariş listesi

`GET /api/orders` yalnız AdminOnly yetkisiyle çağrılır. Sayfalı `OrderSummaryDto` yanıtı, tablo için `customerName` alanını taşır; bu alan müşteri snapshot'ındaki ad ve soyadın birleşimidir, snapshot yoksa `null` olur. İsteğe bağlı `search` parametresi sipariş numarası, müşteri ad/soyadı ve e-posta üzerinde arama yapar.

## Ödeme ve iptal

Üye: `POST /api/orders/{id}/payments`, guest: `POST /api/guest-orders/{id}/payments`. İkisinde `Idempotency-Key` zorunludur ve amount/provider sonucu backend otoritesidir. Aynı siparişte pending ödeme varken yeni deneme reddedilir.

İptal yalnız Pending/Confirmed ve reconciliation bekleyen ödeme yokken mümkündür. İptal mevcut `OrderInventoryService` ile `Cancellation` stok hareketi oluşturur, kupon kullanımını geri alır, rezervasyonu kapatır ve outbox bildirimi üretir.

## Durum ve müşteri aksiyonları

- Pending/Confirmed: ödeme veya uygun iptal.
- Paid/Preparing/Shipped: müşteri iptal edemez; admin yaşam döngüsü.
- Delivered: satın alan üye review/rating ve iade talebi oluşturabilir.
- Guest, claim edilmeden review/rating oluşturamaz. Güvenli claim sonrasında mevcut delivered purchase kontrolü geçerlidir.
- ReturnRequested/ReturnApproved: iade akışı yönetir.
- Cancelled/Refunded: terminal/read-only.
