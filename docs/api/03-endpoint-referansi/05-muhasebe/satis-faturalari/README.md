# Satış faturaları

[API dokümantasyonuna dön](../../../README.md) · [Tam endpoint referansına dön](../../README.md)

Bu bölüm **10 operasyon** içerir. Aradığınız işlemi aşağıdaki görev başlıklarından seçin.

## İşlemler

| Görev | Metot ve endpoint | Yetki |
| --- | --- | --- |
| [Listele](./listele.md) | **GET** `/api/accounting/sales-invoices` | Admin |
| [Oluştur](./olustur.md) | **POST** `/api/accounting/sales-invoices` | Admin |
| [Detayını getir](./detayini-getir.md) | **GET** `/api/accounting/sales-invoices/{id}` | Admin |
| [Güncelle](./guncelle.md) | **PUT** `/api/accounting/sales-invoices/{id}` | Admin |
| [Satış faturasini iptal et](./satis-faturasini-iptal-et.md) | **POST** `/api/accounting/sales-invoices/{id}/cancel` | Admin |
| [Faturaya kalem ekle](./faturaya-kalem-ekle.md) | **POST** `/api/accounting/sales-invoices/{id}/lines` | Admin |
| [Fatura kalemini güncelle](./fatura-kalemini-guncelle.md) | **PUT** `/api/accounting/sales-invoices/{id}/lines/{lineId}` | Admin |
| [Fatura kalemini sil](./fatura-kalemini-sil.md) | **DELETE** `/api/accounting/sales-invoices/{id}/lines/{lineId}` | Admin |
| [Satış faturasini kesinleştir](./satis-faturasini-kesinlestir.md) | **POST** `/api/accounting/sales-invoices/{id}/post` | Admin |
| [Satış siparişinden fatura oluştur](./satis-siparisinden-fatura-olustur.md) | **POST** `/api/accounting/sales-invoices/from-order/{accountingSalesOrderId}` | Admin |
