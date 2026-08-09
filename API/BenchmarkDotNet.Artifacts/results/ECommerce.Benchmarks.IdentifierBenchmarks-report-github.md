```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8973/25H2/2025Update/HudsonValley2)
Intel Core i5-7300HQ CPU 2.50GHz (Kaby Lake), 1 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.300
  [Host]             : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  ShortRun-.NET 10.0 : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3

Job=ShortRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=3  
LaunchCount=1  WarmupCount=3  

```
| Method          | Mean     | Error     | StdDev   | Gen0   | Allocated |
|---------------- |---------:|----------:|---------:|-------:|----------:|
| EncodeProductId | 49.96 ns |  8.653 ns | 0.474 ns | 0.0331 |     104 B |
| DecodeProductId | 87.98 ns | 30.039 ns | 1.647 ns | 0.0331 |     104 B |
