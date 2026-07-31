# ECommerce API Benchmarks

BenchmarkDotNet suite for repeatable CPU, allocation, serialization, pricing, and
catalog query measurements.

Run all benchmarks from the `API` directory:

```powershell
dotnet run -c Release --project benchmarks/ECommerce.Benchmarks
```

Run one benchmark group while iterating:

```powershell
dotnet run -c Release --project benchmarks/ECommerce.Benchmarks -- --filter "*OrderPricing*"
```

Results are written below `BenchmarkDotNet.Artifacts/results`. Always use a Release
build, keep the machine otherwise idle, and compare results on the same hardware.
The product query benchmark uses EF Core's in-memory provider with 1,000 rows so it
is deterministic and requires no development secrets. It measures repository query
composition and EF materialization overhead, not production SQL Server execution
plans, disk I/O, locking, or network latency. Validate those separately against a
production-like SQL Server data set.
