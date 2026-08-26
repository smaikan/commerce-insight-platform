# GET /api/products/by-url/{url}

- Görev alanı: **Katalog → Ürünler → Storefront**.

Yayındaki ürünü güncel URL/slug değeriyle storefront ve SEO kullanımı için döndürür.

## Yetki ve parametre

**Public.** `url` zorunlu path parametresidir.

```http
GET /api/products/by-url/kirmizi-keten-gomlek
```

## Başarılı response — 200 OK

```json
{
  "product": {
    "id": "P00001",
    "title": "Kırmızı Keten Gömlek",
    "mainSku": "GOM-KET-001",
    "url": "kirmizi-keten-gomlek",
    "status": 1,
    "isActive": true,
    "isFeatured": false,
    "hasVariants": true,
    "displayOrder": 10,
    "variants": [],
    "tags": [],
    "collections": [],
    "images": [],
    "clickCount": 0,
    "totalAddToCartCount": 0,
    "totalPurchaseCount": 0,
    "favoriteCount": 0,
    "popularityScore": 0,
    "averageRating": 0,
    "ratingCount": 0,
    "reviewCount": 0
  },
  "images": [],
  "lastModifiedAt": "2026-08-26T10:00:00Z"
}
```

## Hatalar

- `400`: URL biçimi geçersiz
- `404`: ürün yok, aktif değil veya yayında değil

