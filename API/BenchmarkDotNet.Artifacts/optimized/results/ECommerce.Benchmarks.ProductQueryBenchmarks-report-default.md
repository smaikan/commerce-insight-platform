
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8973/25H2/2025Update/HudsonValley2)
Intel Core i5-7300HQ CPU 2.50GHz (Kaby Lake), 1 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.300
  [Host]             : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  ShortRun-.NET 10.0 : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3

Job=ShortRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=3  
LaunchCount=1  WarmupCount=3  

 Method                | Mean     | Error    | StdDev    | Ratio | RatioSD | Gen0     | Gen1    | Allocated  | Alloc Ratio |
---------------------- |---------:|---------:|----------:|------:|--------:|---------:|--------:|-----------:|------------:|
 EntityGraphFirstPage  | 3.083 ms | 2.952 ms | 0.1618 ms |  1.00 |    0.07 | 250.0000 | 93.7500 | 1020.16 KB |        1.00 |
 EntityGraphSearchPage | 1.531 ms | 1.643 ms | 0.0901 ms |  0.50 |    0.03 | 230.4688 | 85.9375 |  913.14 KB |        0.90 |
 ProjectedFirstPage    | 1.503 ms | 3.683 ms | 0.2019 ms |  0.49 |    0.06 | 253.9063 | 85.9375 | 1018.12 KB |        1.00 |
 ProjectedSearchPage   | 1.662 ms | 2.652 ms | 0.1454 ms |  0.54 |    0.05 | 230.4688 | 70.3125 |  923.61 KB |        0.91 |
