# GET /api/main-banner-mobile/admin

- Görev alanı: **Mağaza ve iletişim → Bannerlar → Mobil ana banner**.

Mobil ana banner bölümündeki aktif ve pasif tüm öğeleri yönetim ekranı için döndürür.

## Yetki

**Admin.** Bearer token ve `AdminOnly` policy gerekir.

## Request

```http
GET /api/main-banner-mobile/admin
Authorization: Bearer <admin-access-token>
```

Request body yoktur.

## Başarılı response — 200 OK

```json
{
  "name": "Mobil Ana Banner",
  "key": "main-mobile",
  "items": [
    {
      "id": "a2011c8e-b3f4-48d4-b2cc-572f4d718ab8",
      "name": "Yaz Koleksiyonu",
      "key": "summer-mobile",
      "mediaUrl": "https://cdn.example.com/banners/summer-mobile.webp",
      "mediaType": 0,
      "targetUrl": "/collections/yaz-koleksiyonu",
      "altText": "Yaz koleksiyonunu keşfedin",
      "displayOrder": 1,
      "isActive": false,
      "isMain": true
    }
  ]
}
```

## Hatalar

- `401`: token yok veya geçersiz
- `403`: Admin rolü yok

