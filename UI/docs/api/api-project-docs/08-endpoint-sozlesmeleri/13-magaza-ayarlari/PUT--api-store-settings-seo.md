# PUT /api/store-settings/seo

- Yetki: `AdminOnly`.
- Başarı: `200 AdminStoreSettingsDto`; yeni token.
- Hatalar: `400`, `401`, `403`, `409` ProblemDetails.

`defaultTitle` en çok 200, `titleTemplate` 250, `defaultDescription` 500 karakterdir. `titleTemplate` doluysa tam bir `%s` içerir. `defaultOpenGraphImageUrl` ile `facebookUrl`, `instagramUrl`, `tiktokUrl`, `youtubeUrl`, `xUrl`, `pinterestUrl` nullable mutlak HTTP/HTTPS ve en çok 500 karakterdir. `allowIndexing` ve `expectedConcurrencyToken` zorunludur.

Ürün özel SEO alanları değişmez. Canonical origin request alanı değildir; deployment/environment konfigürasyonunda kalır.
