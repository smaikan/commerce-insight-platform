# Müşteri yönetimi

[API dokümantasyonuna dön](../../../README.md) · [Tam endpoint referansına dön](../../README.md)

Bu bölüm **5 operasyon** içerir. Aradığınız işlemi aşağıdaki görev başlıklarından seçin.

## İşlemler

| Görev | Metot ve endpoint | Yetki |
| --- | --- | --- |
| [Listele](./listele.md) | **GET** `/api/users` | Admin |
| [Detayını getir](./detayini-getir.md) | **GET** `/api/users/{id}` | Admin |
| [Müşterinin siparişlerini listele](./musterinin-siparislerini-listele.md) | **GET** `/api/users/{id}/orders` | Admin |
| [Rolü değiştir](./rolu-degistir.md) | **PATCH** `/api/users/{id}/role` | Admin |
| [Durumu değiştir](./durumu-degistir.md) | **PATCH** `/api/users/{id}/status` | Admin |
