# Yönetim, Stok ve Kampanya

Bu alanların mutasyonları **Admin** yetkisi gerektirir. Public istisnalar yalnız checkout için aktif kargo ve aktif vergi okumalarıdır.

## Dashboard

- `GET /api/dashboard/overview`
- `GET /api/dashboard/product-analytics`

Dashboard değerleri API'nin authoritative projeksiyonudur. İstemci yalnız ekrandaki sayfa satırlarını toplayarak global toplam üretmemelidir.

## Stok hareketleri

Stok bir sayı alanını keyfî biçimde set ederek değil, imzalı hareket defteriyle yönetilir.

```http
POST /api/stock-movements/bulk
Authorization: Bearer <admin-access-token>
```

```json
{
  "items": [
    {
      "productVariantSku": "GOM-KET-KRM-M",
      "quantity": 5,
      "type": 0,
      "description": "Depo sayım düzeltmesi"
    }
  ]
}
```

Toplu işlem atomiktir; satırlardan biri geçersizse tamamı reddedilir. Workflow'a ait hareket türleri manuel seçilemez.

Stok okuma:

- `GET /api/stock-movements`
- `GET /api/stock-movements/variants/{productVariantId}/balance`

## Rezervasyon bakımı

`POST /api/orders/reservations/expire` süresi dolmuş açık rezervasyonları yönetici tarafından tarar. Normal çalışma arka plan worker'ına aittir; bu endpoint manuel operasyon/bakım içindir.

## Kargo yöntemleri

- Public checkout listesi: `GET /api/shipping-methods/active`
- Admin liste/detay/create/update/activation: `/api/shipping-methods`

Checkout request'i yalnız seçilen `shippingMethodId` değerini gönderir; ad ve ücret API tarafından sipariş snapshotına yazılır.

## Vergi oranları

- Public aktif liste: `GET /api/tax-rates/active`
- Admin yönetimi: `/api/tax-rates`

İstemci ürünün vergi veya net fiyatını authoritative olarak hesaplamaz.

## Kuponlar

Kupon yönetimi `/api/coupons` altında Admin erişimindedir. Müşteri kupon uygulamasını sepet/checkout üzerinden kullanır; kuponun indirim tutarını kendisi göndermez.

## Ayrıntılı referans

[Yönetim, stok ve kampanya endpointleri](../03-endpoint-referansi/04-operasyon/README.md)

