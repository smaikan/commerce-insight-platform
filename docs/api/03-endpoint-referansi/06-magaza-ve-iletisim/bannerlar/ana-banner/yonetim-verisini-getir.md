# GET /api/main-banners/admin

- Görev alanı: **Mağaza ve iletişim → Bannerlar → Ana banner**.

- Yetki: **Admin**.
- İşlev: Aktif ve pasif main banner kayıtlarının tamamını yönetim sırasıyla döndürür.
- Response: `BannerSectionDto`.
- Hata: 401 authentication, 403 policy; ortak `ProblemDetails`.

Alanlar için [ortak banner bölüm sözleşmesine](../ortak-sozlesme.md) bakın.
