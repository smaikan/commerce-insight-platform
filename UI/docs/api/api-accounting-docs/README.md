# Accounting Frontend API Dokümantasyonu

Bu klasör, frontend uygulamasının Accounting API'yi kullanması için hazırlanmıştır. Sözleşmeler mevcut controller ve DTO'lara göre yazılmıştır.

## Dosyalar

- `01-genel-kurallar.md`: yetkilendirme, JSON, sayfalama, hata ve idempotency kuralları.
- `02-cari-hesaplar.md`: müşteri/tedarikçi master verisi.
- `03-alis-ve-giderler.md`: alış faturası, stok hareketi tahsisi ve gider dağıtımı.
- `04-satis-siparisleri-ve-faturalar.md`: AccountingSalesOrder ve opsiyonel SalesInvoice.
- `05-odemeler-kasa-banka.md`: tahsilat, tedarikçi ödemesi, kasa, banka ve transferler.
- `06-maliyet-katmanlari.md`: açılış maliyeti, FIFO maliyet geçmişi.
- `07-raporlar.md`: salt-okunur muhasebe raporları.
- `08-muhasebe-is-kurallari-ve-kapsam.md`: açılış, iade, dekont, gider, döviz/vergi, ödeme istisnaları, yetki, dosya, dönem ve entegrasyon kapsamının ayrıntılı sözleşmesi.
- `09-frontend-sozlesmeleri.md`: tam DTO modelleri, validation/uzunluk kuralları, merkezi enumlar, filtreleme, hata kodları, seçim listeleri, UI durum aksiyonları, reversal gösterimi ve rapor kolon sözleşmeleri.

## Önemli mimari not

Accounting satışları e-ticaret `Order` ve `Cart` akışından bağımsızdır. Satış ekranı önce `AccountingSalesOrder` oluşturur; ürün satırları doğrudan `ProductVariantId` ile gönderilir. `SalesInvoice` isteğe bağlı belgedir; stok, FIFO ve müşteri alacağı için ikinci bir etki oluşturmaz.

Tüm Accounting endpointleri yalnız `AdminOnly` yetkisi ile erişilebilir.
