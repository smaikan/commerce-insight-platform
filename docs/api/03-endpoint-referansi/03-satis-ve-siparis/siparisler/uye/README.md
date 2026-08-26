# Üye işlemleri

[API dokümantasyonuna dön](../../../../README.md) · [Tam endpoint referansına dön](../../../README.md)

Bu bölüm **5 operasyon** içerir. Aradığınız işlemi aşağıdaki görev başlıklarından seçin.

## İşlemler

| Görev | Metot ve endpoint | Yetki |
| --- | --- | --- |
| [Oluştur](./olustur.md) | **POST** `/api/orders` | User |
| [Detayını getir](./detayini-getir.md) | **GET** `/api/orders/{id}` | User |
| [Siparişi iptal et](./siparisi-iptal-et.md) | **POST** `/api/orders/{id}/cancel` | User |
| [İptal durumunu getir](./iptal-durumunu-getir.md) | **GET** `/api/orders/{id}/cancellation` | User |
| [Siparislerimi listele](./siparislerimi-listele.md) | **GET** `/api/orders/mine` | User |
