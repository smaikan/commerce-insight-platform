# PUT /api/alt-banner-1

- Görev alanı: **Mağaza ve iletişim → Bannerlar → Alt banner 1**.

- Yetki: **Admin**.
- İşlev: Yalnız Alt Banner 1 bölümünün 0–5 kaydını atomik olarak değiştirir.
- Kural: Tüm kayıtlarda `isMain=false` olmalıdır.
- Response: Güncel `BannerSectionDto`.

Request ve response için [ortak banner bölüm sözleşmesine](../ortak-sozlesme.md) bakın.
