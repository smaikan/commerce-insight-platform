# GET /api/main-banner-mobile

- Görev alanı: **Mağaza ve iletişim → Bannerlar → Mobil ana banner**.

Storefront için mobil ana banner bölümündeki yalnız aktif öğeleri döndürür.

## Yetki

**Public.** Bearer token gerekmez.

## Request

Path, query veya request body yoktur.

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
      "isActive": true,
      "isMain": true
    }
  ]
}
```

