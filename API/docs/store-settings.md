# Typed StoreSettings sözleşmesi

Bu belge mağazanın tek kalıcı ve tip güvenli ayar kaynağını açıklar. `StoreSettings` sabit GUID anahtar, primary key ve `CK_StoreSettings_Singleton` check constraint'iyle veritabanında tek kayıt olarak korunur. Migration başlangıç kaydını ekler; kayıt sonradan silinirse public okuma güvenli varsayılan döndürür, admin okuma kaydı yeniden oluşturur.

## Endpointler ve yetki

| Endpoint | Yetki | Başarı | Amaç |
| --- | --- | --- | --- |
| `GET /api/store-settings` | `AllowAnonymous` | `200 PublicStoreSettingsDto` | Storefront için güvenli ayarlar |
| `GET /api/store-settings/admin` | `AdminOnly` | `200 AdminStoreSettingsDto` | Bütün yönetilebilir alanlar + token |
| `PUT /api/store-settings/identity` | `AdminOnly` | `200 AdminStoreSettingsDto` | Kimlik bölümü |
| `PUT /api/store-settings/contact` | `AdminOnly` | `200 AdminStoreSettingsDto` | İletişim ve görünürlük bölümü |
| `PUT /api/store-settings/legal` | `AdminOnly` | `200 AdminStoreSettingsDto` | Mağazanın yasal şirket bilgileri |
| `PUT /api/store-settings/seo` | `AdminOnly` | `200 AdminStoreSettingsDto` | Global SEO ve sosyal bağlantılar |
| `PUT /api/store-settings/storefront` | `AdminOnly` | `200 AdminStoreSettingsDto` | Çalışma durumu ve katalog tercihleri |

Admin uçlarında anonim istek `401`, Admin rolü olmayan kimlik `403` alır. PUT doğrulama hataları `400`, eski veya eşzamanlı kullanılmış token `409 concurrency_conflict` döndürür. Hata gövdeleri ortak `application/problem+json` sözleşmesidir.

## Concurrency ve bölüm izolasyonu

Client önce admin GET ile `concurrencyToken` alır ve PUT gövdesinde `expectedConcurrencyToken` olarak gönderir. Her başarılı PUT yeni bir `concurrencyToken` döndürür. Aynı tokenla eşzamanlı iki isteğin yalnız biri başarılı olur. Otomatik overwrite veya mutation retry yapılmamalı; `409` sonrasında admin GET ile güncel veri okunup kullanıcı kararı alınmalıdır.

Her PUT yalnız kendi bölümünü değiştirir. Örneğin SEO PUT kimlik, iletişim, yasal ve storefront alanlarını değiştirmez.

## Alan sınırları

| Bölüm | Alanlar | Kural |
| --- | --- | --- |
| Kimlik | `displayName` | Zorunlu, trimlenir, en çok 150 |
| Kimlik | `shortDescription` | Nullable, en çok 500 |
| Kimlik | `logoUrl`, `darkLogoUrl`, `faviconUrl`, `defaultShareImageUrl` | Nullable, mutlak HTTP/HTTPS, en çok 500 |
| İletişim | `supportEmail` | Nullable, geçerli e-posta, en çok 320 |
| İletişim | `supportPhone`, `whatsappNumber` | Nullable, ülke varsayımı olmayan güvenli telefon karakterleri, en çok 30 |
| İletişim | `contactAddress` | Nullable, en çok 1000 |
| İletişim | `workingHours` | Nullable, en çok 500 |
| İletişim | `mapUrl` | Nullable, mutlak HTTP/HTTPS, en çok 500 |
| Yasal | `legalCompanyName` | Nullable, en çok 200 |
| Yasal | `taxOffice`, `country`, `city`, `district` | Nullable, en çok 150 |
| Yasal | `taxNumber`, `nationalIdentityNumber`, `mersisNumber`, `tradeRegistryNumber` | Nullable, checksum tahmini yok; makul harf/rakam/ayraç, en çok 50 |
| Yasal | `addressLine`, `postalCode` | Nullable; sırasıyla en çok 1000 ve 20 |
| SEO | `defaultTitle`, `titleTemplate`, `defaultDescription` | Nullable; sırasıyla en çok 200, 250, 500 |
| SEO/sosyal | bütün URL'ler | Nullable, mutlak HTTP/HTTPS, en çok 500 |
| Storefront | `statusMessage` | Nullable, en çok 500 |
| Storefront | `lowStockThreshold` | 1–1.000.000 pozitif tam sayı |

`titleTemplate` doluysa tam bir adet `%s` yer tutucusu içermelidir. Storefront sayfa başlığını bu yer tutucuya yazar. Canonical storefront origin veritabanı ayarı değildir; deployment/environment konfigürasyonunda kalır ve hiçbir StoreSettings PUT ile değiştirilemez.

## Public ve admin DTO farkı

Public DTO kimlik alanlarını; görünürlük filtresinden geçmiş iletişim alanlarını; SEO, sosyal ve storefront tercihlerini taşır. Bir iletişim görünürlük bayrağı kapalıysa ilgili değer veritabanında korunur fakat public cevapta `null` döner.

Public DTO aşağıdaki alanları hiçbir zaman içermez:

- `legalCompanyName`, `taxOffice`, `taxNumber`
- `nationalIdentityNumber`, `mersisNumber`, `tradeRegistryNumber`
- `country`, `city`, `district`, `addressLine`, `postalCode`
- `concurrencyToken`

Admin DTO bütün yönetilebilir alanları ve concurrency tokenı içerir. StoreSettings entity'si doğrudan HTTP cevabı değildir.

## Enumlar

`StorefrontStatus` numeric wire sözleşmesi:

- `0`: `Active`
- `1`: `Maintenance`
- `2`: `Disabled`

`StorefrontProductSort` numeric wire sözleşmesi:

- `0`: `Newest`
- `1`: `Popularity`
- `2`: `DisplayOrder`
- `3`: `Title`

## Katalog davranışı

- `showOutOfStockProducts=false`: public ürün sorgusu, sayfalama ve `totalCount` öncesinde yalnız en az bir aktif ve stoğu pozitif varyantı olan ürünleri bırakır.
- `showProductsWithoutPrice=false`: public ürün sorgusu, sayfalama öncesinde fiyat gösterebilen aktif varyantı olmayan ürünleri çıkarır. Varyant fiyatının pozitif olması mevcut domain invariant'ıdır.
- Client `sortBy` ve/veya `descending` göndermezse eksik değer mağaza varsayılanından alınır; açıkça gönderilen değer mağaza varsayılanını ezer. Kararlı ikincil ID sıralaması korunur.
- `showCompareAtPrice` yalnız görünüm tercihidir; API mevcut `compareAtPrice` verisini gizleyip eski istemcileri kırmaz.
- `isAvailable`, en az bir aktif varyantın stoğu `> 0` ise true'dur.
- `lowestAvailableStock`, aktif ve stoğu pozitif varyantların minimum stok değeridir; böyle varyant yoksa null'dır. Toplam stok değildir.
- `isLowStock`, `showStockWarning=true` iken en az bir aktif varyantın stoğu `1..lowStockThreshold` aralığındaysa true'dur. Dashboard deployment eşiğiyle ilişkili değildir.

Ayar okuması ürün başına yapılmaz: istek başına tek StoreSettings sorgusu yapılır; ürün filtreleme/projeksiyonları SQL içinde toplu çalışır. Ayrı bir process-local StoreSettings cache'i eklenmemiştir. Mevcut public ürün output cache'i 30 saniyeliktir ve başarılı StoreSettings PUT sonrasında `products` etiketi temizlenir.

`Maintenance` ve `Disabled` durumları public StoreSettings cevabında bildirilir; admin uçlarını durdurmaz. Bu kapsam cart, checkout ve sipariş akışlarını otomatik engellemez; storefront bu durumu bakım/kapalı ekranı göstermek için tüketir.

## Kapsam sınırları

Bu aggregate yerelleştirme, dil, saat dilimi, para birimi, e-fatura, SMTP, sipariş e-postası/metinleri, kargo, vergi, banner veya ödeme sağlayıcılarını içermez. Cloudinary upload/delete yapmaz; yalnız doğrulanmış mutlak medya URL'lerini saklar. Mevcut ürün SEO alanları global varsayımlardan bağımsız kalır ve değişmemiştir. Gelecekte fatura/sipariş belgesinde satıcı bilgisinin tarihsel olarak sabitlenmesi istenirse ayrı bir seller snapshot entegrasyonu gerekir.
