# Satış siparişleri

[API dokümantasyonuna dön](../../../README.md) · [Tam endpoint referansına dön](../../README.md)

Bu bölüm **9 operasyon** içerir. Aradığınız işlemi aşağıdaki görev başlıklarından seçin.

## İşlemler

| Görev | Metot ve endpoint | Yetki |
| --- | --- | --- |
| [Listele](./listele.md) | **GET** `/api/accounting/sales-orders` | Admin |
| [Oluştur](./olustur.md) | **POST** `/api/accounting/sales-orders` | Admin |
| [Detayını getir](./detayini-getir.md) | **GET** `/api/accounting/sales-orders/{id}` | Admin |
| [Güncelle](./guncelle.md) | **PUT** `/api/accounting/sales-orders/{id}` | Admin |
| [Satış siparişini iptal et](./satis-siparisini-iptal-et.md) | **POST** `/api/accounting/sales-orders/{id}/cancel` | Admin |
| [Siparise kalem ekle](./siparise-kalem-ekle.md) | **POST** `/api/accounting/sales-orders/{id}/items` | Admin |
| [Sipariş kalemini güncelle](./siparis-kalemini-guncelle.md) | **PUT** `/api/accounting/sales-orders/{id}/items/{itemId}` | Admin |
| [Sipariş kalemini sil](./siparis-kalemini-sil.md) | **DELETE** `/api/accounting/sales-orders/{id}/items/{itemId}` | Admin |
| [Satış siparişini kesinleştir](./satis-siparisini-kesinlestir.md) | **POST** `/api/accounting/sales-orders/{id}/post` | Admin |
