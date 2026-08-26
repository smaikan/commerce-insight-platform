# Yorumlar

[API dokümantasyonuna dön](../../../../README.md) · [Tam endpoint referansına dön](../../../README.md)

Bu bölüm **3 operasyon** içerir. Aradığınız işlemi aşağıdaki görev başlıklarından seçin.

## İşlemler

| Görev | Metot ve endpoint | Yetki |
| --- | --- | --- |
| [Ürün yorumlarını listele](./urun-yorumlarini-listele.md) | **GET** `/api/product-engagement/products/{productId}/reviews` | Public |
| [Ürün yorumu yaz](./urun-yorumu-yaz.md) | **POST** `/api/product-engagement/products/{productId}/reviews` | User |
| [Yorumu onayla veya reddet](./yorumu-onayla-veya-reddet.md) | **PATCH** `/api/product-engagement/reviews/{reviewId}/approval` | Admin |
