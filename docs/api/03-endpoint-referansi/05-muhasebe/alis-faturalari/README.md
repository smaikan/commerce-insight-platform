# Alış faturaları

[API dokümantasyonuna dön](../../../README.md) · [Tam endpoint referansına dön](../../README.md)

Bu bölüm **13 operasyon** içerir. Aradığınız işlemi aşağıdaki görev başlıklarından seçin.

## İşlemler

| Görev | Metot ve endpoint | Yetki |
| --- | --- | --- |
| [Listele](./listele.md) | **GET** `/api/accounting/purchase-invoices` | Admin |
| [Oluştur](./olustur.md) | **POST** `/api/accounting/purchase-invoices` | Admin |
| [Detayını getir](./detayini-getir.md) | **GET** `/api/accounting/purchase-invoices/{id}` | Admin |
| [Güncelle](./guncelle.md) | **PUT** `/api/accounting/purchase-invoices/{id}` | Admin |
| [Alış faturasini iptal et](./alis-faturasini-iptal-et.md) | **POST** `/api/accounting/purchase-invoices/{id}/cancel` | Admin |
| [Faturanın giderlerini listele](./faturanin-giderlerini-listele.md) | **GET** `/api/accounting/purchase-invoices/{id}/expenses` | Admin |
| [Faturaya gider bağla](./faturaya-gider-bagla.md) | **POST** `/api/accounting/purchase-invoices/{id}/expenses` | Admin |
| [Faturaya kalem ekle](./faturaya-kalem-ekle.md) | **POST** `/api/accounting/purchase-invoices/{id}/lines` | Admin |
| [Fatura kalemini güncelle](./fatura-kalemini-guncelle.md) | **PUT** `/api/accounting/purchase-invoices/{id}/lines/{lineId}` | Admin |
| [Fatura kalemini sil](./fatura-kalemini-sil.md) | **DELETE** `/api/accounting/purchase-invoices/{id}/lines/{lineId}` | Admin |
| [Stok dağıtımlarını güncelle](./stok-dagitimlarini-guncelle.md) | **PUT** `/api/accounting/purchase-invoices/{id}/lines/{lineId}/allocations` | Admin |
| [Alış faturasini kesinleştir](./alis-faturasini-kesinlestir.md) | **POST** `/api/accounting/purchase-invoices/{id}/post` | Admin |
| [Uygun stok hareketlerini listele](./uygun-stok-hareketlerini-listele.md) | **GET** `/api/accounting/purchase-invoices/available-stock-movements` | Admin |
