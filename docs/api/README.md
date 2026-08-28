# E-Commerce API Dokümantasyonu

[Proje ana sayfasına dön](../../README.md)

Bu dokümantasyon, projedeki ASP.NET Core API'yi kullanan geliştiriciler için hazırlanmıştır. Amacı yalnızca endpoint listesini göstermek değil; doğru yetkiyle istek göndermeyi, hata durumlarını yönetmeyi ve temel e-ticaret akışlarını güvenli biçimde kurmayı açıklamaktır.

Dokümantasyon 27 Ağustos 2026 tarihli sözleşmeyi esas alır ve **287 HTTP operasyonunu** kapsar. Örneklerdeki kimlikler, e-posta adresleri ve tokenlar temsilidir.

## Nereden başlamalıyım?

API'yi ilk kez kullanıyorsanız şu sırayı izleyin:

1. [Hızlı başlangıç](01-baslangic/01-hizli-baslangic.md)
2. [Kimlik doğrulama ve yetkilendirme](01-baslangic/02-kimlik-dogrulama-ve-yetkilendirme.md)
3. [Ortak istek ve yanıt kuralları](01-baslangic/03-ortak-kurallar.md)
4. [Hatalar, idempotency ve güvenli tekrar](01-baslangic/04-hatalar-ve-guvenli-tekrar.md)
5. İlgilendiğiniz [iş akışı rehberi](02-is-akislari/README.md)
6. İlgili [endpoint sözleşmesi](03-endpoint-referansi/README.md)

## İş akışı rehberleri

| Alan | Ne zaman okunmalı? |
| --- | --- |
| [Katalog ve ürünler](02-is-akislari/01-katalog-ve-urunler.md) | Storefront ürün listeleme, ürün yönetimi, varyant, görsel ve koleksiyon işlemleri |
| [Sepet, checkout ve sipariş](02-is-akislari/02-sepet-checkout-ve-siparis.md) | Üye veya misafir sepeti, sipariş oluşturma, stok rezervasyonu ve kupon akışı |
| [Ödeme ve sipariş iptali](02-is-akislari/03-odeme-ve-siparis-iptali.md) | iyzico CheckoutForm, callback/webhook, idempotent ödeme ve finansal iptal |
| [İade ve değişim](02-is-akislari/04-iade-ve-degisim.md) | Üye/misafir talebi ve yönetici karar akışı |
| [Yönetim, stok ve kampanya](02-is-akislari/05-yonetim-stok-ve-kampanya.md) | Dashboard, stok hareketi, vergi, kargo ve kupon yönetimi |
| [Muhasebe](02-is-akislari/06-muhasebe.md) | Cari, satış, alış, fatura, ödeme, kasa/banka, maliyet ve raporlar |
| [Mağaza ayarları ve bannerlar](02-is-akislari/07-magaza-ayarlari-ve-bannerlar.md) | Public mağaza kimliği ve yönetilebilir storefront içerikleri |
| [İletişim mesajları](02-is-akislari/08-iletisim-mesajlari.md) | Public iletişim formu ve Admin gelen kutusu |

## Endpoint referansı

[Tam endpoint referansı](03-endpoint-referansi/README.md), operasyonları kullanım amacına göre altı ana alana ayırır:

- **Kimlik ve kullanıcılar:** kayıt, giriş, hesap, oturum, adres ve müşteri yönetimi
- **Katalog:** ürün, varyant, görsel, marka, kategori, koleksiyon, etiket ve ürün etkileşimleri
- **Satış ve sipariş:** sepet, checkout, üye/misafir siparişi, ödeme, iptal, iade ve değişim
- **Operasyon:** dashboard, stok, rezervasyon, kargo, vergi ve kupon işlemleri
- **Muhasebe:** satış/alış belgeleri, cari, tahsilat, kasa/banka, maliyet ve raporlar
- **Mağaza ve iletişim:** mağaza ayarları, bannerlar, iletişim formu ve gelen kutusu

Her ana alan önce kaynağa, ardından yapılacak göreve ayrılır. Örneğin sipariş iptali için `Satış ve sipariş → Siparişler → Üye işlemleri → siparişi iptal et`; ürün kârlılığı için `Muhasebe → Raporlar → Kârlılık raporları → ürün kârlılığını getir` yolunu izleyebilirsiniz. Dosya adları HTTP route kopyaları değil, doğrudan görevi anlatan Türkçe adlardır.

Her endpoint sayfasında mümkün olduğunda şunlar bulunur:

- HTTP metodu ve route
- Gerekli kullanıcı/rol veya guest güvenlik bağlamı
- Path, query ve header parametreleri
- Request body alanları ve JSON örneği
- Başarılı response body örneği
- Beklenen hata durumları
- Concurrency, idempotency veya yaşam döngüsü notları

## Yetki gösterimi

| Etiket | Anlamı |
| --- | --- |
| **Public** | Bearer token gerekmez. Rate limit, Origin veya bot koruması yine uygulanabilir. |
| **Guest session** | JWT gerekmez; API'nin ürettiği HttpOnly guest cookie ve operasyona göre CSRF/Origin doğrulaması gerekir. |
| **User** | Geçerli access token gerekir. Kaynak sahipliği API tarafından ayrıca doğrulanır. |
| **Admin** | Geçerli access token ve `AdminOnly` policy gerekir. |
| **Provider** | Ödeme sağlayıcısından gelen imzalı callback veya webhook isteğidir. |

## Sözleşme dosyaları

- [OpenAPI JSON](openapi.json): makine tarafından okunabilir güncel wire sözleşmesi
- [Endpoint referansı](03-endpoint-referansi/README.md): insan tarafından okunabilir ayrıntılı sözleşmeler
- [Temel enumlar ve kimlik biçimleri](01-baslangic/05-kimlikler-ve-enumlar.md)
- [Dokümantasyon bakım rehberi](BAKIM.md): sözleşme değiştiğinde izlenecek güncelleme ve doğrulama adımları

## Kaynak doğruluğu

Bu dizin hazırlanırken `UI/docs/api` altındaki mevcut sözleşmeler, güncel OpenAPI çıktısı ve controller yetki attribute'ları birlikte incelenmiştir. Route veya DTO değiştiğinde hem `openapi.json` hem de ilgili Markdown sayfası güncellenmelidir. OpenAPI ile çalışma zamanı arasında fark görülürse istemci varsayım üretmemeli; controller/DTO davranışı doğrulanmalı ve dokümantasyon farkı kapatılmalıdır.
