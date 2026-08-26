# PUT /api/alt-banner-2

- Görev alanı: **Mağaza ve iletişim → Bannerlar → Alt banner 2**.

- Yetki: **Admin**.
- İşlev: Yalnız Alt Banner 2 bölümünün 0–5 kaydını atomik olarak değiştirir.
- Kural: Tüm kayıtlarda `isMain=false` olmalıdır.
- Response: Güncel `BannerSectionDto`.

Request ve response için [ortak banner bölüm sözleşmesine](../ortak-sozlesme.md) bakın.
