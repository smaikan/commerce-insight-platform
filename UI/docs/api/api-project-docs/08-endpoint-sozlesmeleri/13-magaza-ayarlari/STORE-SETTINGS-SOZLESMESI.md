# StoreSettings frontend entegrasyon sözleşmesi

StoreSettings tek mağaza için sabit kimlikli, typed ve concurrency korumalı kaynaktır. Client hiçbir endpointte StoreSettings kimliği göndermez. Genel kurallar, alan sınırları ve storefront davranışları için API kaynağı: `API/docs/store-settings.md`.

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
