```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8973/25H2/2025Update/HudsonValley2)
Intel Core i5-7300HQ CPU 2.50GHz (Kaby Lake), 1 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.300
  [Host]             : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  ShortRun-.NET 10.0 : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3

Job=ShortRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=3  
LaunchCount=1  WarmupCount=3  

```
| Method     | Mean     | Error    | StdDev    | Ratio | RatioSD | Gen0     | Gen1    | Allocated  | Alloc Ratio |
|----------- |---------:|---------:|----------:|------:|--------:|---------:|--------:|-----------:|------------:|
| FirstPage  | 1.281 ms | 2.571 ms | 0.1409 ms |  1.01 |    0.13 | 269.5313 | 74.2188 | 1018.03 KB |        1.00 |
| SearchPage | 1.350 ms | 2.985 ms | 0.1636 ms |  1.06 |    0.15 | 222.6563 | 78.1250 |  912.93 KB |        0.90 |
