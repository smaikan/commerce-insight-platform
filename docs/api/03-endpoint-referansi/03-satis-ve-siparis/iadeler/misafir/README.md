# Misafir işlemleri

[API dokümantasyonuna dön](../../../../README.md) · [Tam endpoint referansına dön](../../../README.md)

Bu bölüm **3 operasyon** içerir. Aradığınız işlemi aşağıdaki görev başlıklarından seçin.

## İşlemler

| Görev | Metot ve endpoint | Yetki |
| --- | --- | --- |
| [Siparişin iade taleplerini listele](./siparisin-iade-taleplerini-listele.md) | **GET** `/api/guest-orders/{id}/returns` | Guest session |
| [Misafir iade talebi oluştur](./misafir-iade-talebi-olustur.md) | **POST** `/api/guest-orders/{id}/returns` | Guest session |
| [Misafir iade talebi detayını getir](./misafir-iade-talebi-detayini-getir.md) | **GET** `/api/guest-orders/{id}/returns/{returnId}` | Guest session |
