```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8973/25H2/2025Update/HudsonValley2)
Intel Core i5-7300HQ CPU 2.50GHz (Kaby Lake), 1 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.300
  [Host]             : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  ShortRun-.NET 10.0 : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3

Job=ShortRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=3  
LaunchCount=1  WarmupCount=3  

```
| Method     | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|------:|--------:|---------:|---------:|----------:|------------:|
| FirstPage  | 3.524 ms | 0.5099 ms | 0.0280 ms |  1.00 |    0.01 | 492.1875 | 335.9375 |   2.48 MB |        1.00 |
| SearchPage | 5.354 ms | 1.9338 ms | 0.1060 ms |  1.52 |    0.03 | 750.0000 | 734.3750 |   4.49 MB |        1.81 |
| DeepPage   | 3.659 ms | 6.2675 ms | 0.3435 ms |  1.04 |    0.08 | 492.1875 | 328.1250 |   2.48 MB |        1.00 |
