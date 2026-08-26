# Dokümantasyon Bakım Rehberi

Bu dizin GitHub üzerinde yayınlanan insan odaklı API dokümantasyonudur. API sözleşmesi değiştiğinde doküman aynı geliştirme kapsamında güncellenmelidir.

## Güncelleme sırası

1. Çalışan API'den güncel OpenAPI çıktısını üretin.
2. `docs/api/openapi.json` dosyasını yeni çıktıyla değiştirin.
3. Eklenen/değişen her operasyonun ilgili `03-endpoint-referansi/<ana-alan>/<kaynak>/<görev>.md` sayfasını güncelleyin. Dosya adı route kopyası değil, `siparisi-iptal-et.md` gibi insanın arayacağı Türkçe görev adı olmalıdır.
4. Route, yetki veya iş akışı değiştiyse ilgili `01-baslangic` ve `02-is-akislari` rehberlerini güncelleyin.
5. Bölüm indekslerini yeniden üretin:

   ```bash
   node docs/api/tools/build-reference.mjs
   ```

6. OpenAPI şemasına göre JSON örneklerindeki dizi/nesne biçimlerini normalize edin:

   ```bash
   node docs/api/tools/normalize-examples.mjs
   ```

7. Kapsam, link ve JSON doğrulamasını çalıştırın:

   ```bash
   node docs/api/tools/validate.mjs
   ```

## Endpoint sayfası standardı

Yeni bir endpoint sayfası en az şu başlıkları taşımalıdır:

1. `# METHOD /api/route`
2. Klasör yoluyla uyumlu `Görev alanı` izi
3. Kısa ve gerçek işlev açıklaması
4. Yetki seviyesi ve sahiplik/guest/provider koşulu
5. Path, query ve özel header parametreleri
6. Request body alan tablosu ve geçerli JSON örneği; body yoksa açık ifade
7. Başarılı HTTP status ve response JSON örneği; `204` ise body olmadığının açıklaması
8. İlgili `400/401/403/404/409/429` ve özel ProblemDetails kodları
9. Varsa idempotency, concurrency, cache ve yaşam döngüsü etkileri

Örnekler gerçek token, parola, cookie, müşteri PII'si veya ödeme verisi içermemelidir.

## Klasörleme standardı

Endpointler yalnızca şu altı ana iş alanından birine yerleştirilir:

- `01-kimlik-ve-kullanicilar`
- `02-katalog`
- `03-satis-ve-siparis`
- `04-operasyon`
- `05-muhasebe`
- `06-magaza-ve-iletisim`

Ana alanın altında önce kaynak, gerekiyorsa erişim bağlamı veya alt görev bulunur. Örneğin üye sipariş iptali `03-satis-ve-siparis/siparisler/uye/siparisi-iptal-et.md`, ürün kârlılık raporu `05-muhasebe/raporlar/karlilik/urun-karliligini-getir.md` altında tutulur. Yeni klasörün `README.md` indeksi `build-reference.mjs` ile üretilir; endpoint hiçbir zaman doğrudan ana alan köküne veya teknik controller adına göre yerleştirilmez.

## Sözleşme farkı

OpenAPI, Markdown ve controller/DTO davranışı çelişirse fark gizlenmez. Önce runtime davranışı doğrulanır, ardından OpenAPI ve Markdown birlikte düzeltilir. İstemcinin eksik sözleşmeden alan, enum, rol veya retry davranışı tahmin etmesi beklenmez.
