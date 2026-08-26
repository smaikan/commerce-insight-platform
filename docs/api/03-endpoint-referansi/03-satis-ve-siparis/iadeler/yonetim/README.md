# Yönetim

[API dokümantasyonuna dön](../../../../README.md) · [Tam endpoint referansına dön](../../../README.md)

Bu bölüm **6 operasyon** içerir. Aradığınız işlemi aşağıdaki görev başlıklarından seçin.

## İşlemler

| Görev | Metot ve endpoint | Yetki |
| --- | --- | --- |
| [Listele](./listele.md) | **GET** `/api/returns` | Admin |
| [İade talebini onayla](./iade-talebini-onayla.md) | **POST** `/api/returns/{id}/approve` | Admin |
| [İade talebini tamamla](./iade-talebini-tamamla.md) | **POST** `/api/returns/{id}/complete` | Admin |
| [İade ürününü teslim al](./iade-urununu-teslim-al.md) | **POST** `/api/returns/{id}/receive` | Admin |
| [İade talebini reddet](./iade-talebini-reddet.md) | **POST** `/api/returns/{id}/reject` | Admin |
| [İade yönetim detayını getir](./iade-yonetim-detayini-getir.md) | **GET** `/api/returns/admin/{id}` | Admin |
