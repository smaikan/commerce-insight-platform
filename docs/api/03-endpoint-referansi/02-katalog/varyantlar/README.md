# Varyantlar

[API dokümantasyonuna dön](../../../README.md) · [Tam endpoint referansına dön](../../README.md)

Bu bölüm **9 operasyon** içerir. Aradığınız işlemi aşağıdaki görev başlıklarından seçin.

## İşlemler

| Görev | Metot ve endpoint | Yetki |
| --- | --- | --- |
| [Detayını getir](./detayini-getir.md) | **GET** `/api/product-variants/{id}` | Public |
| [Güncelle](./guncelle.md) | **PUT** `/api/product-variants/{id}` | Admin |
| [Sil](./sil.md) | **DELETE** `/api/product-variants/{id}` | Admin |
| [Varyant aktifligini güncelle](./varyant-aktifligini-guncelle.md) | **PATCH** `/api/product-variants/{id}/activation` | Admin |
| [Varyant fiyatini güncelle](./varyant-fiyatini-guncelle.md) | **PATCH** `/api/product-variants/{id}/price` | Admin |
| [Ürünün varyantlarını listele](./urunun-varyantlarini-listele.md) | **GET** `/api/product-variants/by-product/{productId}` | Public |
| [Ürüne varyant ekle](./urune-varyant-ekle.md) | **POST** `/api/product-variants/by-product/{productId}` | Admin |
| [Ürünün varyantlarını toplu güncelle](./urunun-varyantlarini-toplu-guncelle.md) | **PUT** `/api/product-variants/by-product/{productId}/bulk` | Admin |
| [Varyant stok hareketi oluştur](./varyant-stok-hareketi-olustur.md) | **POST** `/api/product-variants/stock-movements` | Admin |
