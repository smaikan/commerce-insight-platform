# API Performans Raporu

Ölçüm tarihi: 31 Temmuz 2026

Ortam: .NET 10.0.8, BenchmarkDotNet 0.15.8, Windows 11, Intel Core i5-7300HQ
2.50 GHz, 4 fiziksel çekirdek. `ShortRun` işi 1 launch, 3 warmup ve 3 ölçüm
iterasyonu kullandı. Sonuçlar aynı makinedeki değişiklikleri karşılaştırmak için bir
başlangıç çizgisidir; kapasite/SLA sonucu olarak yorumlanmamalıdır.

## Ölçüm sonuçları

| Senaryo | Ortalama | Allocation |
|---|---:|---:|
| Public ürün ID encode | 49,77 ns | 104 B |
| Public ürün ID decode | 85,71 ns | 104 B |
| Ürün URL üretimi, 20 karakter | 194,4 ns | 336 B |
| Ürün URL üretimi, 79 karakter | 520,0 ns | 912 B |
| Fiyatlandırma, 1 satır | 270,7 ns | 592 B |
| Fiyatlandırma, 10 satır | 2,684 µs | 1.968 B |
| Fiyatlandırma, 100 satır | 25,563 µs | 14.424 B |
| Fiyat sonucu JSON, 1 satır | 873,1 ns | 608 B |
| Fiyat sonucu JSON, 10 satır | 3,067 µs | 1.536 B |
| Fiyat sonucu JSON, 100 satır | 26,133 µs | 10.721 B |
| Katalog ilk sayfa, 1.000 kayıttan 20 ürün | 1,281 ms | 1.018,03 KB |
| Katalog arama sayfası, 1.000 kayıt | 1,350 ms | 912,93 KB |

Katalog sorgusu EF Core InMemory sağlayıcısıyla ölçüldü. Sonuç repository sorgu
kurma/materialization maliyetini gösterir; SQL Server execution planı, disk I/O,
kilitlenme ve ağ gecikmesini içermez.

## Değerlendirme

Kimlik kodlama, URL üretimi, fiyatlandırma ve JSON serileştirme mevcut sınırlar için
sağlıklı görünüyor. Sepetin 100 satır sınırında fiyat hesaplama ve sonucu JSON'a
çevirme ayrı ayrı yaklaşık 26 µs; bunları optimize etmek şu anda anlamlı bir iş
etkisi yaratmaz.

Ana risk katalog listeleme yoludur. Mevcut sorgu 20 kayıt döndürmek için Product
entity'lerini Type, Brand, TaxRate, Variants ve ProductTags/Tag grafikleriyle
materialize ediyor, sonra Application katmanında DTO'ya map ediyor. `AsSplitQuery`
ve `CountAsync` nedeniyle SQL Server'da tipik olarak toplam sayım, ana kayıt,
varyantlar ve etiketler için birden fazla round-trip oluşur. InMemory ölçümündeki
yaklaşık 1 MB allocation bu geniş nesne grafiğinin maliyetini görünür kılıyor.

## Uygulanan iyileştirmeler ve yeni ölçüm

Katalog endpoint'i artık entity grafiğini yükleyip sonradan map etmek yerine, aynı
`ProductDto` sözleşmesi için doğrudan bir read-model projection kullanır. Anonim
ürün liste ve detay GET yanıtları 30 saniye output cache ile saklanır; ürün oluşturan
veya değiştiren tüm mevcut endpoint'ler başarılı işlemden sonra `products` cache
etiketini temizler. HTTPS JSON yanıtları için response compression da etkinleştirildi.

| Senaryo | Önce | Sonra | Sonuç |
|---|---:|---:|---|
| İlk katalog sayfası, 20 ürün | 3,083 ms | 1,503 ms | yaklaşık %51 daha hızlı |
| Arama sayfası | 1,531 ms | 1,662 ms | ShortRun örnekleminde anlamlı fark yok |

Bu karşılaştırma aynı 1.000 kayıtlı EF Core InMemory verisiyle yapıldı. InMemory
sağlayıcısı tarama/materialization davranışında SQL Server'dan ayrıldığı için
allocation sonucu (yaklaşık 1 MB) ve arama sonucu production kapasite tahmini olarak
kullanılmamalıdır. Buna rağmen ilk sayfa ölçümü, endpoint'in artık entity grafiği
oluşturmadığı read-model yolunun CPU maliyetini belirgin biçimde düşürdüğünü gösterir.

## Öncelikli iyileştirme alanları

1. Katalog liste endpoint'i için yalnızca listede gereken kolonları seçen özel bir
   read model/projection oluşturulmalı. Detay DTO'su yerine daha küçük bir
   `ProductListItemDto` kullanılması ve collection grafiklerinin yalnızca detay
   endpoint'inde yüklenmesi en yüksek getirili değişikliktir.
2. JWT `OnTokenValidated` her authenticated istekte
   `IsAccessTokenValidAsync` ile veritabanına gidiyor. Güvenlik gereksinimine göre
   kısa süreli dağıtık cache veya daha kısa access-token ömrüyle round-trip
   azaltılabilir. Cache kullanılırsa logout/security-version değişikliklerinde
   invalidation ve kabul edilen revocation gecikmesi açıkça tasarlanmalıdır.
3. Public katalog GET cevaplarında output cache ve response compression yok.
   Query-string'e göre anahtarlanan kısa TTL, değişikliklerde tag invalidation ve
   sıkıştırma özellikle anonim trafik ve büyük varyant/etiket payload'larında
   uygulama ve veritabanı yükünü azaltır.
4. `Contains` tabanlı Title/Url/MainSku araması SQL Server'da başında joker bulunan
   `LIKE` üretip normal B-tree indekslerinden yararlanamayabilir. Üretim verisiyle
   execution plan alınmalı; ihtiyaca göre full-text search veya normalize edilmiş
   prefix arama kullanılmalıdır.
5. Her sayfalı listede `CountAsync` ayrı sorgu çalıştırıyor ve yüksek sayfa
   numaralarında `Skip` maliyeti büyür. Toplam sayının opsiyonel olması ve yoğun
   akışlarda `(sort column, Id)` keyset pagination değerlendirilmelidir.
6. Tek kolonlu indeksler mevcut olsa da varsayılan filtre+sıralama için üretim
   execution planına göre bileşik indeks düşünülmelidir. Örneğin aktif katalog
   trafiğinde Status/IsActive ile PopularityScore/Id kombinasyonu ölçülmeden doğrudan
   migration'a eklenmemelidir.

## Sonraki doğrulama

Production benzeri SQL Server kopyasında gerçek veri hacmiyle katalog, login,
authenticated katalog ve checkout akışları ayrı ayrı yük testine alınmalı; p50,
p95, p99, throughput, hata oranı, SQL logical reads ve sorgu sayısı birlikte
izlenmelidir. Bu benchmark süiti kod seviyesindeki regresyonları ölçer; eşzamanlı
HTTP kapasite testinin yerine geçmez.
