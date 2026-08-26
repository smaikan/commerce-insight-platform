# POST /api/orders/import/bulk

- Görev alanı: **Satış ve sipariş → Siparişler → Dış sistem aktarımı**.

Dış sistemden birden fazla siparişi tek transaction içinde atomik ve tekrar güvenli biçimde içe aktarır.

## Yetki

**Admin.** Bearer token ve `AdminOnly` policy gerekir.

## Request body

```json
{
  "orders": [
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
      "applyInventoryAndMetrics": true
    }
  ]
}
```

## Başarılı response — 201 Created

```json
[
  {
    "order": {
      "id": "3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26",
      "orderNumber": "EXT-20260826-1001",
      "status": 2,
      "grandTotal": 1349.8,
      "items": [],
      "payments": []
    },
    "wasImported": true
  }
]
```

Bir satır geçersizse batch'in tamamı geri alınır.

## Hatalar

- `400`: boş batch, geçersiz sipariş veya toplam ilişkisi
- `401`: token yok/geçersiz
- `403`: Admin rolü yok
- `404`: bağlı kaynaklardan biri bulunamadı
- `409`: dış kimlik veya iş kuralı çatışması

