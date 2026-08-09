
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8973/25H2/2025Update/HudsonValley2)
Intel Core i5-7300HQ CPU 2.50GHz (Kaby Lake), 1 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.300
  [Host]             : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  ShortRun-.NET 10.0 : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3

Job=ShortRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=3  
LaunchCount=1  WarmupCount=3  

 Method   | Title                | Mean     | Error    | StdDev  | Gen0   | Allocated |
--------- |--------------------- |---------:|---------:|--------:|-------:|----------:|
 **Generate** | **Basic Cotton T-Shirt** | **194.4 ns** | **16.27 ns** | **0.89 ns** | **0.1070** |     **336 B** |
 **Generate** | **Premi(...)Cable [79]** | **520.0 ns** | **53.31 ns** | **2.92 ns** | **0.2899** |     **912 B** |
 **Generate** | **Turkc(...) 2026 [32]** | **258.9 ns** | **70.73 ns** | **3.88 ns** | **0.1221** |     **384 B** |
