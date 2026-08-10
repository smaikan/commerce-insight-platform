# PUT /api/main-banners

- Yetki: `AdminOnly`.
- İşlev: Main Banner bölümünün 0–5 kaydını atomik olarak değiştirir.
- Kural: Liste doluysa tam olarak bir aktif kayıt `isMain=true` olmalıdır; bu kayıt response'ta `displayOrder=0` olur.
- Response: Güncel `BannerSectionDto`.
- Hata: 400 validation/domain, 401 authentication, 403 policy; ortak `ProblemDetails`.

Request ve response için [ortak banner bölüm sözleşmesine](BANNER-BOLUM-SOZLESMESI.md) bakın.
