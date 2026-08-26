# POST /api/orders/import

- Görev alanı: **Satış ve sipariş → Siparişler → Dış sistem aktarımı**.

Dış sistemden tek siparişi Admin yetkisiyle içe aktarır. `orderNumber` idempotent dış kimlik olarak kullanılır.

## Yetki

**Admin.** Bearer token ve `AdminOnly` policy gerekir.

## Request body

```json
{
  "orderNumber": "EXT-20260826-1001",
  "userId": 42,
  "subTotal": 1299.9,
  "discountTotal": 0,
  "shippingTotal": 49.9,
  "taxTotal": 118.17,
  "grandTotal": 1349.8,
  "status": 2,
  "items": [
    {
      "productId": "P00001",
      "productVariantId": "f16b53be-2ddd-47f9-b247-a8605c67f65b",
      "productTitle": "Kırmızı Keten Gömlek",
      "variantSku": "GOM-KET-KRM-M",
      "unitPrice": 1299.9,
      "quantity": 1,
      "discountTotal": 0,
      "taxRatePercentage": 10,
      "taxTotal": 118.17
    }
  ],
  "createdAtUtc": "2026-08-26T11:30:00Z",
  "couponCode": null,
  "shippingMethodId": "3bb1cb68-27dc-45a1-9a73-78b1356dbfb2",
  "shippingMethodName": "Standart Kargo",
  "paymentProvider": 1,
  "paymentTransactionId": "external-payment-1001",
  "applyInventoryAndMetrics": true
}
```

> Bu entegrasyon endpointinin güncel wire sözleşmesinde `userId` iç veritabanı `int64` kimliğidir. Genel public `U...` kullanıcı kimliği kuralından farklıdır; dış entegrasyon istemcisi bu alanı varsaymamalı, sistem sahibiyle eşleme sözleşmesini netleştirmelidir.

## Başarılı response

- `201 Created`: sipariş ilk kez içe aktarıldı.
- `200 OK`: aynı `orderNumber` daha önce içe aktarılmıştı; mevcut sonuç idempotent olarak döndü.

```json
{
  "order": {
    "id": "3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26",
    "orderNumber": "EXT-20260826-1001",
    "status": 2,
    "subTotal": 1299.9,
    "discountTotal": 0,
    "shippingTotal": 49.9,
    "taxTotal": 118.17,
    "grandTotal": 1349.8,
    "items": [],
    "payments": []
  },
  "wasImported": true
}
```

## Hatalar

- `400`: body veya toplam tutar ilişkileri geçersiz
- `401`: token yok/geçersiz
- `403`: Admin rolü yok
- `404`: eşlenen kullanıcı, ürün, varyant veya kargo kaynağı yok
- `409`: aynı dış kimlikle çelişen veri veya durum kuralı

