# GET /api/main-banners

- Görev alanı: **Mağaza ve iletişim → Bannerlar → Ana banner**.

- Yetki: **Public**.
- İşlev: En fazla 5 aktif main banner medyasını döndürür.
- Response: `BannerSectionDto`; `name=Main Banner`, `key=main-banner`.
- Sıralama: `isMain=true` kayıt ilk, kalanlar `displayOrder` sırasındadır.
- Boş durum: `{ "name": "Main Banner", "key": "main-banner", "items": [] }`.

Alanlar için [ortak banner bölüm sözleşmesine](../ortak-sozlesme.md) bakın.
