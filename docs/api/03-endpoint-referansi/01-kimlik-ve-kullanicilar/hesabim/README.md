# Hesabım

[API dokümantasyonuna dön](../../../README.md) · [Tam endpoint referansına dön](../../README.md)

Bu bölüm **8 operasyon** içerir. Aradığınız işlemi aşağıdaki görev başlıklarından seçin.

## İşlemler

| Görev | Metot ve endpoint | Yetki |
| --- | --- | --- |
| [Hesabımı getir](./hesabimi-getir.md) | **GET** `/api/users/me` | User |
| [Hesabımı sil](./hesabimi-sil.md) | **DELETE** `/api/users/me` | User |
| [E-posta adresimi değiştir](./e-posta-adresimi-degistir.md) | **PUT** `/api/users/me/email` | User |
| [Parolamı değiştir](./parolami-degistir.md) | **PUT** `/api/users/me/password` | User |
| [Profilimi güncelle](./profilimi-guncelle.md) | **PUT** `/api/users/me/profile` | User |
| [Oturumlarimi listele](./oturumlarimi-listele.md) | **GET** `/api/users/me/sessions` | User |
| [Diger oturumları kapat](./diger-oturumlari-kapat.md) | **DELETE** `/api/users/me/sessions` | User |
| [Oturumu kapat](./oturumu-kapat.md) | **DELETE** `/api/users/me/sessions/{sessionId}` | User |
