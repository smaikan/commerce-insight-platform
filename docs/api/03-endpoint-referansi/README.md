# Tam Endpoint Referansı

[API dokümantasyonuna dön](../README.md)

Bu referans güncel OpenAPI sözleşmesindeki **287 operasyonun tamamını** kapsar. Her bölümde endpoint, erişim seviyesi, kısa amaç ve ayrıntılı sözleşme bağlantısı bulunur.

| Bölüm | Operasyon |
| --- | ---: |
| [Kimlik ve kullanıcılar](./01-kimlik-ve-kullanicilar/README.md) | 25 |
| [Katalog](./02-katalog/README.md) | 77 |
| [Satış ve sipariş](./03-satis-ve-siparis/README.md) | 43 |
| [Operasyon](./04-operasyon/README.md) | 23 |
| [Muhasebe](./05-muhasebe/README.md) | 84 |
| [Mağaza ve iletişim](./06-magaza-ve-iletisim/README.md) | 35 |

## Sık aranan görevler

| Yapmak istediğiniz iş | Doğrudan bağlantı |
| --- | --- |
| Kullanıcı kaydı veya giriş | [Kimlik doğrulama](./01-kimlik-ve-kullanicilar/kimlik-dogrulama/README.md) |
| Storefront ürünlerini listeleme ve arama | [Storefront ürünleri](./02-katalog/urunler/storefront/README.md) |
| Ürün oluşturma, güncelleme veya yayımlama | [Ürün yönetimi](./02-katalog/urunler/yonetim/README.md) |
| Varyant ve stok/fiyat işlemleri | [Varyantlar](./02-katalog/varyantlar/README.md) |
| Sepete ürün ekleme veya sepeti güncelleme | [Sepet](./03-satis-ve-siparis/sepet/README.md) |
| Üye siparişi oluşturma veya iptal etme | [Üye siparişleri](./03-satis-ve-siparis/siparisler/uye/README.md) |
| Misafir siparişini görüntüleme veya iptal etme | [Misafir siparişleri](./03-satis-ve-siparis/siparisler/misafir/README.md) |
| iyzico ödeme formunu başlatma | [iyzico ödemeleri](./03-satis-ve-siparis/odemeler/iyzico/README.md) |
| İade/değişim talebi ve yönetimi | [İade ve değişim](./03-satis-ve-siparis/iadeler/README.md) |
| Stok, rezervasyon, kargo, vergi veya kupon | [Operasyon](./04-operasyon/README.md) |
| Fatura, cari, kasa/banka veya finansal rapor | [Muhasebe](./05-muhasebe/README.md) |
| Banner veya mağaza ayarlarını yönetme | [Mağaza ve iletişim](./06-magaza-ve-iletisim/README.md) |
| İletişim formundan mesaj gönderme | [İletişim formu](./06-magaza-ve-iletisim/iletisim-formu/README.md) |
| Gelen iletişim mesajını yanıtlama | [İletişim yönetimi](./06-magaza-ve-iletisim/iletisim-yonetimi/README.md) |

## Yetki etiketleri

- **Public:** Bearer token gerekmez.
- **Guest session / Guest checkout:** HttpOnly guest cookie ve endpoint bazında CSRF, Origin veya idempotency koruması gerekir.
- **User:** Bearer token ve kaynak sahipliği gerekir.
- **Admin:** Bearer token ve `AdminOnly` policy gerekir.
- **Provider:** Kullanıcı tokenı yerine sağlayıcı imzası/tokenı doğrulanır.

Ayrıntılar için [kimlik doğrulama ve yetkilendirme rehberini](../01-baslangic/02-kimlik-dogrulama-ve-yetkilendirme.md) okuyun.
