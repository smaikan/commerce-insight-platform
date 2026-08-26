# PUT /api/store-settings/storefront

- Görev alanı: **Mağaza ve iletişim → Mağaza ayarları**.

- Yetki: **Admin**.
- Başarı: `200 AdminStoreSettingsDto`; yeni token.
- Hatalar: `400`, `401`, `403`, `409` ProblemDetails.

Request alanları: numeric `status`, nullable `statusMessage` (500), `showOutOfStockProducts`, `showProductsWithoutPrice`, numeric `defaultProductSort`, `defaultProductSortDescending`, `showCompareAtPrice`, `showStockWarning`, `lowStockThreshold` (1–1.000.000) ve `expectedConcurrencyToken`.

`status`: `0 Active`, `1 Maintenance`, `2 Disabled`. Admin endpointleri bütün durumlarda çalışır; bu ayar cart/checkout/order API'lerini bu kapsamda engellemez. Katalog filtre, sıralama ve stok özeti semantiği [ortak sözleşmede](ortak-sozlesme.md) açıklanmıştır.

Geçerli request ve ortak admin response örnekleri için [StoreSettings ortak sözleşmesindeki storefront örneğine](ortak-sozlesme.md) bakın.
