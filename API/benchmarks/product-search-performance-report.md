# Public ürün arama performans raporu

## Ortam ve karar

Ölçüm 14 Ağustos 2026 tarihinde SQL Server Express LocalDB 17.0.4025.3 üzerinde yapıldı. Veritabanı collation değeri `SQL_Latin1_General_CP1_CI_AS`, `FULLTEXTSERVICEPROPERTY('IsFullTextInstalled')` sonucu `0` idi. Bu nedenle deployment'a Full-Text Search zorunluluğu eklenmedi; normalize arama dokümanı ve indeksli iki/üç karakterli gram aday tablosu seçildi.

Temsili veri 100.000 ürün, ürün başına iki aktif varyant, bir ana görsel, iki koleksiyon ve üç etiketten oluşur. `product-search-candidate-comparison.sql` doğrudan ilişkisel `%term%` sorgusunu denormalize doküman taramasıyla; `product-search-gram-candidate.sql` ise 2.000.000 temsili gram satırında indeksli aday sorgusunu karşılaştırır.

## SQL ölçüm özeti

| Aday | Logical read / gözlem | CPU | Elapsed | Plan sonucu |
|---|---:|---:|---:|---|
| İlişkisel title/brand/type/collection/tag araması | İlişki tablolarında yaklaşık 1,6 milyon read | yaklaşık 2.469 ms | cold koşuda 16 sn'ye kadar | Çoklu ilişki taraması ve satır çoğalması |
| Tek satır arama dokümanı taraması | 5.916 read | yaklaşık 1.395 ms | warm yaklaşık 1.395 ms | Join çoğalması yok, fakat doküman scan devam ediyor |
| Gram adayı + doküman doğrulaması | Gram 3, doküman cold 681 / warm 358 read | cold 15 ms / warm 16 ms | cold 19 ms / warm 1 ms | Gram indeks seek ile küçük aday kümesi |

Suggestion sorgusu `TOP (Limit + 1)` kullanır, toplam sonuç `COUNT` sorgusu çalıştırmaz ve kart verisini tek SQL komutunda projekte eder. Tam arama mevcut sayfalama sözleşmesi gereği count ve items olmak üzere iki komuttur.

## HTTP ölçümü

55 ürünlü development veritabanında, cache kullanmadan çalışan API üzerinde ilk EF/JIT'li suggestion isteği 2.780 ms; sonraki tek/çok token ve SKU suggestion istekleri 103 ms ve altı, sıcak tekrar 22 ms ölçüldü. Örnek suggestion payload 350 byte'tır. Bunlar küçük development verisi sonuçlarıdır ve ölçek kabul kanıtı değildir.

Production-benzeri 100.000 ürünlü API + SQL Server ortamında paralel uçtan uca test çalıştırılamadığı için sıcak API p95/p99 sonucu **ölçülemedi**. Dolayısıyla p95 ≤150 ms ve p99 ≤300 ms hedefleri başarılı kabul edilmemiştir. Tekrarlanabilir harness:

```powershell
.\benchmarks\scripts\measure-published-product-search.ps1 `
  -BaseUrl http://127.0.0.1:3300 `
  -Iterations 200 `
  -Concurrency 16
```

PowerShell 7 paralel ölçüm yapar. Windows PowerShell 5 ortamında script uyumluluk amacıyla sıralı çalışır ve raporda etkin concurrency değerini `1` gösterir.
