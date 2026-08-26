# Misafir işlemleri

[API dokümantasyonuna dön](../../../../README.md) · [Tam endpoint referansına dön](../../../README.md)

Bu bölüm **7 operasyon** içerir. Aradığınız işlemi aşağıdaki görev başlıklarından seçin.

## Görev alanları

| Alan | Operasyon |
| --- | ---: |
| [Misafir sipariş erişimi](./erisim/README.md) | 2 |
| [Hesaba devretme](./hesaba-devret/README.md) | 1 |

## İşlemler

| Görev | Metot ve endpoint | Yetki |
| --- | --- | --- |
| [Listele](./listele.md) | **GET** `/api/guest-orders` | Guest session |
| [Detayını getir](./detayini-getir.md) | **GET** `/api/guest-orders/{id}` | Guest session |
| [Misafir siparişini iptal et](./misafir-siparisini-iptal-et.md) | **POST** `/api/guest-orders/{id}/cancel` | Guest session |
| [Misafir iptal durumunu getir](./misafir-iptal-durumunu-getir.md) | **GET** `/api/guest-orders/{id}/cancellation` | Guest session |
