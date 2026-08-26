# PUT /api/store-settings/identity

- Görev alanı: **Mağaza ve iletişim → Mağaza ayarları**.

- Yetki: **Admin**.
- Başarı: `200 AdminStoreSettingsDto`; yeni `concurrencyToken`.
- Hatalar: `400`, `401`, `403`, `409` ProblemDetails.

| Alan | Zorunlu | Kural |
| --- | --- | --- |
| `displayName` | Evet | Trimlenir, en çok 150 |
| `shortDescription` | Hayır | Nullable, en çok 500 |
| `logoUrl`, `darkLogoUrl`, `faviconUrl`, `defaultShareImageUrl` | Hayır | Nullable, mutlak HTTP/HTTPS, en çok 500 |
| `expectedConcurrencyToken` | Evet | Güncel admin GET/PUT tokenı |

Yalnız kimlik alanlarını değiştirir.

Geçerli request ve ortak admin response örnekleri için [StoreSettings ortak sözleşmesine](ortak-sozlesme.md) bakın.
