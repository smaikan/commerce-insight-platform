# PATCH /api/products/{id}/activation

- Görev alanı: **Katalog → Ürünler → Yönetim**.

Ürünün aktiflik durumunu değiştirir ve storefront ürün cache'ini geçersiz kılar.

## Yetki ve parametre

**Admin.** `id`, `P...` biçiminde public Product ID'dir.

```http
PATCH /api/products/P00001/activation
Authorization: Bearer <admin-access-token>
Content-Type: application/json
```

## Request body

```json
{
  "isActive": false
}
```

## Başarılı response — 200 OK

Güncel `ProductDto` döner:

```json
{
  "id": "P00001",
  "title": "Kırmızı Keten Gömlek",
  "mainSku": "GOM-KET-001",
  "url": "kirmizi-keten-gomlek",
  "status": 1,
  "isActive": false,
  "isFeatured": false,
  "hasVariants": true,
  "displayOrder": 10,
  "clickCount": 1200,
  "totalAddToCartCount": 140,
  "totalPurchaseCount": 36,
  "favoriteCount": 82,
  "popularityScore": 5820,
  "averageRating": 4.7,
  "ratingCount": 54,
  "reviewCount": 21,
  "variants": [],
  "tags": [],
  "collections": [],
  "images": []
}
```

## Hatalar

- `400`: Product ID/body geçersiz
- `401`: token yok/geçersiz
- `403`: Admin rolü yok
- `404`: ürün bulunamadı
- `409`: ürün mevcut durumda değiştirilemiyor

