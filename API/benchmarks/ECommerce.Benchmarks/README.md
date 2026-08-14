# ECommerce API Benchmarks

BenchmarkDotNet suite for repeatable CPU, allocation, serialization, pricing, and
catalog and order-list query measurements.

Run all benchmarks from the `API` directory:

```powershell
dotnet run -c Release --project benchmarks/ECommerce.Benchmarks
```

Run one benchmark group while iterating:

```powershell
dotnet run -c Release --project benchmarks/ECommerce.Benchmarks -- --filter "*OrderPricing*"
```

Run the order list projection benchmark:

```powershell
dotnet run -c Release --project benchmarks/ECommerce.Benchmarks -- --filter "*OrderList*"
```

Results are written below `BenchmarkDotNet.Artifacts/results`. Always use a Release
build, keep the machine otherwise idle, and compare results on the same hardware.
The product query benchmark uses EF Core's in-memory provider with 1,000 rows so it
is deterministic and requires no development secrets. It measures repository query
composition and EF materialization overhead, not production SQL Server execution
plans, disk I/O, locking, or network latency. Validate those separately against a
production-like SQL Server data set.

The order list benchmark also uses EF Core's in-memory provider with 1,000 orders.
It covers the exact `OrderListReader` projection, page count and `Skip` behavior,
but it cannot validate SQL Server indexes or `Contains` query plans.

## Public ürün araması

SQL Server aday planlarını 100.000 ürün ve gerçekçi ilişki hacmiyle karşılaştırmak için:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i benchmarks/sql/product-search-candidate-comparison.sql -o .artifacts/product-search-candidate-comparison.txt
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -i benchmarks/sql/product-search-gram-candidate.sql -o .artifacts/product-search-gram-candidate.txt
```

İlk script ilişkisel `%term%` sorgusu ile tek satırlık doküman taramasını; ikinci script iki milyon satırlık temsili gram indeksinde hibrit aday sorgusunu ölçer. `STATISTICS IO/TIME` ham çıktıları `.artifacts` altında oluşur. Veri hazırlama süresi sorgu süresinden ayrı değerlendirilir.

Production-benzeri API ve SQL Server aynı kontrollü ortamdayken p95/p99 ölçümü için önce 100.000 ürünlük veri ve migration hazırlanır, API cache'siz suggestion endpointiyle başlatılır, ardından:

```powershell
pwsh benchmarks/scripts/measure-published-product-search.ps1 -BaseUrl https://localhost:3300 -Iterations 1000 -Concurrency 32
```

LocalDB, geliştirme verisi veya paylaşımlı geliştirici makinesi p95/p99 kabul kararı için yeterli değildir. Script sık, seyrek ve sonuçsuz sorguları paralel gönderir; 429 oluşmaması için performans ortamındaki rate-limit ölçüm profili bilinçli biçimde yapılandırılmalıdır.
