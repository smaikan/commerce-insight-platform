# İletişim yönetimi

[API dokümantasyonuna dön](../../../README.md) · [Tam endpoint referansına dön](../../README.md)

Bu bölüm **6 operasyon** içerir. Aradığınız işlemi aşağıdaki görev başlıklarından seçin.

## İşlemler

| Görev | Metot ve endpoint | Yetki |
| --- | --- | --- |
| [Listele](./listele.md) | **GET** `/api/contact-messages` | Admin |
| [Mesaj detayını getir](./mesaj-detayini-getir.md) | **GET** `/api/contact-messages/{id}` | Admin |
| [Mesajı ata](./mesaji-ata.md) | **PATCH** `/api/contact-messages/{id}/assignment` | Admin |
| [Dahili not ekle](./dahili-not-ekle.md) | **POST** `/api/contact-messages/{id}/notes` | Admin |
| [Müşteriye yanıt gönder](./musteriye-yanit-gonder.md) | **POST** `/api/contact-messages/{id}/replies` | Admin |
| [Mesaj durumunu güncelle](./mesaj-durumunu-guncelle.md) | **PATCH** `/api/contact-messages/{id}/status` | Admin |

## Ortak sözleşmeler

- [İletişim mesajları ortak sözleşmesi](./ortak-sozlesme.md)
