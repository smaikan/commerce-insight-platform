# PUT /api/products/performance-metrics

- Görev alanı: **Katalog → Ürünler → Yönetim**.

Harici katalog kaynağındaki ürün performans sayaçlarını Admin yetkisiyle topluca eşitler.

## Yetki

**Admin.** Bearer token ve `AdminOnly` policy gerekir.

## Request body

```json
{
  "items": [
    {
      "productId": "P00001",
      "clickCount": 1200,
      "totalAddToCartCount": 140,
      "totalPurchaseCount": 36,
      "favoriteCount": 82,
      "averageRating": 4.7,
      "ratingCount": 54,
      "reviewCount": 21
    }
  ]
}
```

`popularityScore` request'te gönderilmez; API kendi ağırlıklarıyla türetir.

## Başarılı response — 200 OK

Güncellenen ürünler `ProductDto[]` olarak döner:

```json
[
  {
    "id": "P00001",
    "title": "Kırmızı Keten Gömlek",
    "mainSku": "GOM-KET-001",
    "url": "kirmizi-keten-gomlek",
    "status": 1,
    "isActive": true,
    "isFeatured": false,
    "hasVariants": true,
    "displayOrder": 10,
    "clickCount": 1200,
    "totalAddToCartCount": 140,
    "totalPurchaseCount": 36,
    "favoriteCount": 82,
    "popularityScore": 3368,
    "averageRating": 4.7,
    "ratingCount": 54,
    "reviewCount": 21,
    "variants": [],
    "tags": [],
    "collections": [],
    "images": []
  }
]
```

Başarı public ürün cache'ini geçersiz kılar.

## Hatalar

- `400`: sayaç, puan veya Product ID geçersiz
- `401`: token yok/geçersiz
- `403`: Admin rolü yok
- `404`: ürünlerden biri bulunamadı
