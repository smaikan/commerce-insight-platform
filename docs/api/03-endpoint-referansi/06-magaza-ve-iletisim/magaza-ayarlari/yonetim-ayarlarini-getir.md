# GET /api/store-settings/admin

- Görev alanı: **Mağaza ve iletişim → Mağaza ayarları**.

- Yetki: **Admin**.
- Başarı: `200 AdminStoreSettingsDto`.
- Hatalar: anonim `401`, Admin olmayan kimlik `403`.

Bütün kimlik, iletişim/görünürlük, yasal şirket, SEO/sosyal ve storefront alanlarını; ayrıca güncel `concurrencyToken` değerini döndürür. Client bu tokenı sonraki section PUT gövdesinde `expectedConcurrencyToken` olarak gönderir. Client StoreSettings kimliği göndermez.

Response örneği için [StoreSettings ortak sözleşmesine](ortak-sozlesme.md) bakın.
