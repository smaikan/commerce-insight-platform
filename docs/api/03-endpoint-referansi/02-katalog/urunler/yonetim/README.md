# Yönetim

[API dokümantasyonuna dön](../../../../README.md) · [Tam endpoint referansına dön](../../../README.md)

Bu bölüm **12 operasyon** içerir. Aradığınız işlemi aşağıdaki görev başlıklarından seçin.

## İşlemler

| Görev | Metot ve endpoint | Yetki |
| --- | --- | --- |
| [Listele](./listele.md) | **GET** `/api/products` | Admin |
| [Oluştur](./olustur.md) | **POST** `/api/products` | Admin |
| [Detayını getir](./detayini-getir.md) | **GET** `/api/products/{id}` | Admin |
| [Güncelle](./guncelle.md) | **PUT** `/api/products/{id}` | Admin |
| [Sil](./sil.md) | **DELETE** `/api/products/{id}` | Admin |
| [Aktiflik durumunu güncelle](./aktiflik-durumunu-guncelle.md) | **PATCH** `/api/products/{id}/activation` | Admin |
| [Öne çıkarma durumunu güncelle](./one-cikarma-durumunu-guncelle.md) | **PATCH** `/api/products/{id}/featured` | Admin |
| [Varyant kullanımını güncelle](./varyant-kullanimini-guncelle.md) | **PATCH** `/api/products/{id}/has-variants` | Admin |
| [Ürün ilişkilerini güncelle](./urun-iliskilerini-guncelle.md) | **PUT** `/api/products/{id}/relations` | Admin |
| [Yayın durumunu güncelle](./yayin-durumunu-guncelle.md) | **PATCH** `/api/products/{id}/status` | Admin |
| [Toplu ürün oluştur](./toplu-urun-olustur.md) | **POST** `/api/products/bulk` | Admin |
| [Performans metriklerini güncelle](./performans-metriklerini-guncelle.md) | **PUT** `/api/products/performance-metrics` | Admin |
