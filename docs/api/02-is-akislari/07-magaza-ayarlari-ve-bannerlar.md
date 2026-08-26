# Mağaza Ayarları ve Bannerlar

Storefront içerikleri iki ayrı yapı üzerinden sunulur:

- Tekil mağaza kimliği/iletişim/yasal/SEO/storefront ayarları
- Ana, mobil ana ve alt banner bölümleri

## Mağaza ayarları

Public görünüm:

```http
GET /api/store-settings
```

Bu endpoint yalnız storefront için güvenli alanları döndürür. Yönetim görünümü ve güncellemeler Admin erişimindedir:

- `GET /api/store-settings/admin`
- `PUT /api/store-settings/identity`
- `PUT /api/store-settings/contact`
- `PUT /api/store-settings/legal`
- `PUT /api/store-settings/seo`
- `PUT /api/store-settings/storefront`

Her bölüm ayrı güncellenir; istemci başka bölümün değerlerini tahmin ederek body'ye katmamalıdır.

## Banner bölümleri

Her bölüm aynı temel sözleşmeyi kullanır:

- Public `GET`: yalnız aktif öğeler
- Admin `GET /admin`: aktif/pasif tüm öğeler
- Admin `PUT`: bölümü atomik olarak değiştirir

Bölümler:

- `/api/main-banners`
- `/api/main-banner-mobile`
- `/api/alt-banner-1` ... `/api/alt-banner-5`

Örnek PUT body:

```json
{
  "items": [
    {
      "name": "Yaz Koleksiyonu",
      "key": "summer-hero",
      "mediaUrl": "https://cdn.example.com/banners/summer.webp",
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

Bir bölüm en fazla beş medya kaydı taşır. `isMain` semantiği yalnız ilgili bölüm sözleşmesine göre kullanılmalıdır. Güncelleme sonrası storefront banner cache'i API tarafından geçersiz kılınır.

## Ayrıntılı referans

- [Katalog ve banner endpointleri](../03-endpoint-referansi/02-katalog/README.md)
- [Mağaza ayarları endpointleri](../03-endpoint-referansi/06-magaza-ve-iletisim/magaza-ayarlari/README.md)

