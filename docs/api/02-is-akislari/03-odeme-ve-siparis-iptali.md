# Ödeme ve Sipariş İptali

Ödeme akışı iyzico CheckoutForm kullanır. API kart bilgisi almaz; hosted ödeme oturumu üretir.

## CheckoutForm başlatma

- Üye: `POST /api/orders/{id}/payments/iyzico/checkout-form`
- Misafir: `POST /api/guest-orders/{id}/payments/iyzico/checkout-form`

Her iki işlemde de `Idempotency-Key` kullanılır. Guest endpointi ayrıca session, CSRF ve trusted Origin ister.

Örnek response:

```json
{
  "paymentId": "c67d9fd7-c70d-4e86-b430-594f097a53cd",
  "orderId": "3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26",
  "token": "<provider-session-token>",
  "paymentPageUrl": "https://sandbox-cpp.iyzipay.com?...",
  "expiresAt": "2026-08-26T12:15:00Z"
}
```

## Sonucun kesinleştirilmesi

- Callback: `POST /api/payments/iyzico/callback`
- Webhook: `POST /api/payments/iyzico/webhook`

Browserın geri dönmesi veya ödeme sayfasından ayrılması tek başına başarılı/başarısız ödeme kanıtı değildir. API, iyzico tokenıyla retrieve yapar; imza, sipariş, para birimi ve tutar eşleşirse ödemeyi kesinleştirir.

Provider sonucu belirsizse stok hemen serbest bırakılmaz; arka plan mutabakatı olası tahsilatı kontrol eder.

## Ödenmemiş sipariş iptali

`Pending` veya `Confirmed` siparişte provider tahsilatı olmadığı kesinleştirilirse:

- Sipariş `Cancelled` olur.
- Stok rezervasyonu geri alınır.
- Kupon kullanımı serbest bırakılır.
- Bekleyen ödeme kapatılır.
- Sepet otomatik geri oluşturulmaz.

## Ödenmiş sipariş iptali

`Paid` veya `Preparing` siparişte API önce gerçek finansal ters işlemi tamamlar:

- Aynı iş gününde iyzico cancel denenir.
- Gerekirse gerçek item `paymentTransactionId` değerleriyle refund yapılır.
- Para sağlayıcıda geri alınmadan sipariş yalnız veritabanında `Cancelled` yapılmaz.

`Shipped` ve sonraki siparişler normal sipariş iptaliyle kapatılamaz; iade/değişim akışına geçer.

## 200 ve 202 ayrımı

`POST /api/orders/{id}/cancel` veya guest karşılığı:

- `200 OK`: iptal tamamen bitmiştir; güncel `OrderDto` döner.
- `202 Accepted`: provider sonucu henüz kesin değildir; iptal operasyonu döner.

```json
{
  "operationId": "3470e031-3fc8-42af-9755-f0fcae2b06cb",
  "orderId": "3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26",
  "status": 2,
  "reversalType": 0,
  "nextAttemptAt": "2026-08-26T12:01:00Z",
  "pollingUrl": "/api/orders/3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26/cancellation"
}
```

İstemci `202` sonrasında yeni finansal iptal isteği göndermek yerine `pollingUrl` adresini kontrollü aralıklarla okur.

## Önemli conflict kodları

| `code` | Anlam |
| --- | --- |
| `order_cancellation_not_allowed` | Sipariş kargoya verilmiş veya daha ileri durumda |
| `payment_reversal_data_missing` | Güvenli ters işlem için eski provider/item verisi eksik |
| `payment_reversal_rejected` | Provider ters işlemi kesin reddetti |
| `payment_reversal_manual_review` | Finansal bütünlük otomatik doğrulanamadı |

## Ayrıntılı referans

[Sipariş ve ödeme endpointleri](../03-endpoint-referansi/03-satis-ve-siparis/README.md)

