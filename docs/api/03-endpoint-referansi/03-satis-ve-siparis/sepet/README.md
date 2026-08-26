# Sepet

[API dokümantasyonuna dön](../../../README.md) · [Tam endpoint referansına dön](../../README.md)

Bu bölüm **7 operasyon** içerir. Aradığınız işlemi aşağıdaki görev başlıklarından seçin.

## Görev alanları

| Alan | Operasyon |
| --- | ---: |
| [Sepet kalemleri](./kalemler/README.md) | 3 |
| [Sepet kuponu](./kupon/README.md) | 1 |
| [Sepet oturum devri](./oturum-devri/README.md) | 1 |

## İşlemler

| Görev | Metot ve endpoint | Yetki |
| --- | --- | --- |
| [Listele](./listele.md) | **GET** `/api/cart` | Public / guest cart |
| [Sepeti temizle](./sepeti-temizle.md) | **DELETE** `/api/cart` | Public / guest cart |

## Ortak sözleşmeler

- [CartItemDto ana görsel sözleşmesi](./kalem-ana-gorsel-sozlesmesi.md)
- [Sepet ve sipariş varyant snapshot sözleşmesi](./varyant-snapshot-sozlesmesi.md)
