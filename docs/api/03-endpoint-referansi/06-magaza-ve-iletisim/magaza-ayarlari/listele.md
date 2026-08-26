# GET /api/store-settings

- Görev alanı: **Mağaza ve iletişim → Mağaza ayarları**.

- Yetki: **Public**.
- Başarı: `200 PublicStoreSettingsDto`.
- Cache: StoreSettings için ayrı process-local cache yoktur.

Kimlik; görünürlük filtresinden geçmiş iletişim; SEO, sosyal ve storefront alanlarını döndürür. Gizli iletişim değerleri `null` olur. Yasal/vergi alanları ve `concurrencyToken` response şemasında bulunmaz. `status` numeric olarak `0 Active`, `1 Maintenance`, `2 Disabled` değerlerinden biridir.

Ayrıntılı alan ve davranış tablosu: [StoreSettings frontend entegrasyon sözleşmesi](ortak-sozlesme.md).
