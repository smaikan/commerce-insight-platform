# PUT /api/store-settings/legal

- Görev alanı: **Mağaza ve iletişim → Mağaza ayarları**.

- Yetki: **Admin**.
- Başarı: `200 AdminStoreSettingsDto`; yeni token.
- Hatalar: `400`, `401`, `403`, `409` ProblemDetails.

Nullable alanlar: `legalCompanyName` (200), `taxOffice` (150), `taxNumber`, `nationalIdentityNumber`, `mersisNumber`, `tradeRegistryNumber` (her biri 50), `country`/`city`/`district` (150), `addressLine` (1000), `postalCode` (20). `expectedConcurrencyToken` zorunludur.

Numaralarda tahmini checksum kuralı yoktur. Bunlar mağazanın şirket bilgileridir; müşteri adresi veya Accounting CurrentAccount değildir ve public DTO'ya çıkmaz.

Geçerli request ve ortak admin response örnekleri için [StoreSettings ortak sözleşmesine](ortak-sozlesme.md) bakın.
