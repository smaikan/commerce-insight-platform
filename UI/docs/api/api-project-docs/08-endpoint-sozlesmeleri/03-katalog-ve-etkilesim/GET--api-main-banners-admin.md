# GET /api/main-banners/admin

- Yetki: `AdminOnly`.
- İşlev: Aktif ve pasif main banner kayıtlarının tamamını yönetim sırasıyla döndürür.
- Response: `BannerSectionDto`.
- Hata: 401 authentication, 403 policy; ortak `ProblemDetails`.

Alanlar için [ortak banner bölüm sözleşmesine](BANNER-BOLUM-SOZLESMESI.md) bakın.
