# StoreSettings frontend entegrasyon sözleşmesi

StoreSettings tek mağaza için sabit kimlikli, tip güvenli ve concurrency korumalı kaynaktır. İstemci hiçbir endpointte StoreSettings kimliği göndermez; bütün alan sınırları ve storefront davranışları bu bölümdeki sözleşmelerde açıklanır.

## Endpoint özeti

| Metot ve route | Yetki | Response |
| --- | --- | --- |
| `GET /api/store-settings` | Anonim (`security: []`) | `PublicStoreSettingsDto` |
| `GET /api/store-settings/admin` | `AdminOnly` | `AdminStoreSettingsDto` |
| `PUT /api/store-settings/identity` | `AdminOnly` | `AdminStoreSettingsDto` |
| `PUT /api/store-settings/contact` | `AdminOnly` | `AdminStoreSettingsDto` |
| `PUT /api/store-settings/legal` | `AdminOnly` | `AdminStoreSettingsDto` |
| `PUT /api/store-settings/seo` | `AdminOnly` | `AdminStoreSettingsDto` |
| `PUT /api/store-settings/storefront` | `AdminOnly` | `AdminStoreSettingsDto` |

Admin GET bütün yönetilebilir alanları ve `concurrencyToken` değerini döndürür. Her PUT gövdesi `expectedConcurrencyToken` taşır ve yalnız kendi bölümünü değiştirir. Başarılı PUT yeni tokenlı tam admin DTO döndürür. `409 concurrency_conflict` sonrasında otomatik mutation retry/overwrite yapılmaz; admin GET ile güncel veri okunur.

## Güncelleme request örnekleri

Her örnekteki `expectedConcurrencyToken`, son `GET /api/store-settings/admin` veya başarılı PUT cevabından alınır.

### Mağaza kimliği

```json
{
  "displayName": "Eleven Store",
  "shortDescription": "Seçkin ürünler, güvenli alışveriş.",
  "logoUrl": "https://cdn.example.com/store/logo.svg",
  "darkLogoUrl": "https://cdn.example.com/store/logo-dark.svg",
  "faviconUrl": "https://cdn.example.com/store/favicon.ico",
  "defaultShareImageUrl": "https://cdn.example.com/store/share.webp",
  "expectedConcurrencyToken": "a77a263f-79f2-4f99-afbd-55fe2f1159c8"
}
```

### İletişim bilgileri

```json
{
  "supportEmail": "destek@example.com",
  "supportPhone": "+90 212 555 01 01",
  "whatsappNumber": "+90 530 555 01 01",
  "contactAddress": "Örnek Mahallesi, İstanbul",
  "workingHours": "Pazartesi-Cuma 09.00-18.00",
  "mapUrl": "https://maps.example.com/store",
  "showSupportEmail": true,
  "showSupportPhone": true,
  "showWhatsapp": true,
  "showContactAddress": true,
  "showWorkingHours": true,
  "showMap": true,
  "expectedConcurrencyToken": "a77a263f-79f2-4f99-afbd-55fe2f1159c8"
}
```

### Yasal bilgiler

```json
{
  "legalCompanyName": "Eleven Mağazacılık A.Ş.",
  "taxOffice": "Örnek Vergi Dairesi",
  "taxNumber": "1234567890",
  "nationalIdentityNumber": null,
  "mersisNumber": "0123456789012345",
  "tradeRegistryNumber": "123456",
  "country": "Türkiye",
  "city": "İstanbul",
  "district": "Şişli",
  "addressLine": "Örnek Mahallesi No: 11",
  "postalCode": "34381",
  "expectedConcurrencyToken": "a77a263f-79f2-4f99-afbd-55fe2f1159c8"
}
```

### SEO ve sosyal bağlantılar

```json
{
  "defaultTitle": "Eleven Store",
  "titleTemplate": "%s | Eleven Store",
  "defaultDescription": "Seçkin ürünleri güvenli alışveriş deneyimiyle keşfedin.",
  "defaultOpenGraphImageUrl": "https://cdn.example.com/store/og-default.webp",
  "allowIndexing": true,
  "facebookUrl": "https://www.facebook.com/example",
  "instagramUrl": "https://www.instagram.com/example",
  "tiktokUrl": null,
  "youtubeUrl": null,
  "xUrl": null,
  "pinterestUrl": null,
  "expectedConcurrencyToken": "a77a263f-79f2-4f99-afbd-55fe2f1159c8"
}
```

### Storefront tercihleri

```json
{
  "status": 0,
  "statusMessage": null,
  "showOutOfStockProducts": false,
  "showProductsWithoutPrice": false,
  "defaultProductSort": 0,
  "defaultProductSortDescending": true,
  "showCompareAtPrice": true,
  "showStockWarning": true,
  "lowStockThreshold": 5,
  "expectedConcurrencyToken": "a77a263f-79f2-4f99-afbd-55fe2f1159c8"
}
```

## Admin response örneği

Aşağıdaki kısaltılmış örnek, bütün section PUT endpointlerinin ortak response biçimini gösterir. Diğer yönetilebilir alanlar da aynı DTO içinde yer alır.

```json
{
  "displayName": "Eleven Store",
  "supportEmail": "destek@example.com",
  "showSupportEmail": true,
  "legalCompanyName": "Eleven Mağazacılık A.Ş.",
  "defaultTitle": "Eleven Store",
  "allowIndexing": true,
  "status": 0,
  "showOutOfStockProducts": false,
  "defaultProductSort": 0,
  "defaultProductSortDescending": true,
  "showCompareAtPrice": true,
  "showStockWarning": true,
  "lowStockThreshold": 5,
  "concurrencyToken": "6554c4ce-9f2c-4a58-a167-af7fc2f8aa87"
}
```

## Public güvenlik sınırı

Public DTO'da yasal şirket, vergi, TCKN, MERSİS, ticaret sicili, şirket adresi ve concurrency alanları yoktur. İletişim görünürlüğü kapalıysa ilgili değer `null` döner; frontend gizli değeri hiç almaz.

## Enumlar

- `StorefrontStatus`: `0 Active`, `1 Maintenance`, `2 Disabled`.
- `StorefrontProductSort`: `0 Newest`, `1 Popularity`, `2 DisplayOrder`, `3 Title`.

Enumlar JSON wire üzerinde sayısaldır.

## SEO ve canonical

`titleTemplate` nullable'dır; doluysa tam bir `%s` içerir. `allowIndexing` yalnız storefront'un tükettiği global tercihtir; API robots.txt üretmez. Canonical storefront origin StoreSettings alanı değildir ve deployment/environment konfigürasyonundan okunmalıdır.

## Ürün listeleme etkisi

- Stok/fiyat görünürlük filtreleri SQL'de, sayfalama ve `totalCount` öncesinde uygulanır.
- Query'de `sortBy`/`descending` yoksa StoreSettings varsayılanı; varsa client değeri kullanılır.
- `isAvailable`: aktif varyantlardan en az birinin stoğu `> 0`.
- `lowestAvailableStock`: aktif ve stoğu pozitif varyantların minimumu; toplam stok değildir.
- `isLowStock`: uyarı açıkken aktif varyantlardan en az birinin stoğu `1..lowStockThreshold` aralığında.
- `showCompareAtPrice` görünüm tercihidir; `compareAtPrice` API ürün DTO'sundan kaldırılmaz.

## Hata modeli

PUT uçları `200`, `400`, `401`, `403`, `409`; admin GET `200`, `401`, `403` bekler. Hatalar `application/problem+json` gövdesinde en az `status`, `code`, `detail`, `traceId`, `timestamp`; validation hatasında ayrıca `errors` taşır.
