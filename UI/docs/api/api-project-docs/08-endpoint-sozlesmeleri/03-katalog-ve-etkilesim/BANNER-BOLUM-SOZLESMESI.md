# Banner bölüm sözleşmesi

Main Banner ile Alt Banner 1–5 birbirinden bağımsız kaynaklardır. Her bölüm en fazla 5 kayıt taşır; boş `items` bölümü temizler ve diğer bölümlere dokunmaz.

## PUT request

```json
{
  "items": [
    {
      "name": "Yaz kampanyası",
      "key": "summer-campaign",
      "mediaUrl": "https://cdn.example.com/banners/summer.mp4",
      "mediaType": 2,
      "targetUrl": "/collections/yaz",
      "altText": "Yaz koleksiyonu",
      "displayOrder": 4,
      "isActive": true,
      "isMain": true
    }
  ]
}
```

- `items`: Opsiyonel; `null` veya `[]` bölümü temizler, en fazla 5 kayıt.
- `name`: Zorunlu, en fazla 150 karakter.
- `key`: Zorunlu, bölüm içinde büyük-küçük harften bağımsız benzersiz; en fazla 100 karakter ve `[A-Za-z0-9][A-Za-z0-9_-]*` biçimi.
- `mediaUrl`: Zorunlu mutlak HTTP/HTTPS URL, en fazla 500 karakter.
- `mediaType`: `Image=1`, `Video=2`.
- `targetUrl`: Opsiyonel `/...` uygulama yolu veya mutlak HTTP/HTTPS URL; en fazla 500 karakter.
- `altText`: Opsiyonel, en fazla 500 karakter.
- `displayOrder`: Sıfır veya pozitif ve bölüm içinde benzersiz. Backend sonucu `0..n-1` biçiminde normalize eder.
- `isActive`: Public GET yalnız `true` kayıtları döndürür.
- `isMain`: Yalnız Main Banner bölümünde kullanılabilir. Dolu main bölümünde tam olarak bir aktif öğe `true` olmalı; seçilen öğe otomatik ilk sıraya taşınır. Alt bölümlerde yalnız `false` geçerlidir.

## Response

```json
{
  "name": "Main Banner",
  "key": "main-banner",
  "items": [
    {
      "id": "8c87048a-e108-49a6-9c7e-faa1f0498ad2",
      "name": "Yaz kampanyası",
      "key": "summer-campaign",
      "mediaUrl": "https://cdn.example.com/banners/summer.mp4",
      "mediaType": 2,
      "targetUrl": "/collections/yaz",
      "altText": "Yaz koleksiyonu",
      "displayOrder": 0,
      "isActive": true,
      "isMain": true
    }
  ]
}
```

Public GET aktif kayıtları; `/admin` GET aktif ve pasif kayıtları döndürür. Tüm yazmalar `AdminOnly` ve serializable transaction ile atomiktir. Hatalar ortak `ProblemDetails` biçimindedir.
