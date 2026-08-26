# PUT /api/main-banner-mobile

- Görev alanı: **Mağaza ve iletişim → Bannerlar → Mobil ana banner**.

Mobil ana banner bölümünü diğer banner bölümlerine dokunmadan atomik olarak değiştirir.

## Yetki

**Admin.** Bearer token ve `AdminOnly` policy gerekir.

## Request body

`items` en fazla beş banner öğesi taşır. Boş liste bölümü temizler.

```json
{
  "items": [
    {
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

Başarıdan sonra storefront banner cache'i API tarafından geçersiz kılınır.

## Hatalar

- `400`: alan, medya tipi veya beş öğe sınırı geçersiz
- `401`: token yok veya geçersiz
- `403`: Admin rolü yok

