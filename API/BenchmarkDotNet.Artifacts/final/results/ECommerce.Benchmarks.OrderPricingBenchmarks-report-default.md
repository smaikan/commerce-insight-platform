
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8973/25H2/2025Update/HudsonValley2)
Intel Core i5-7300HQ CPU 2.50GHz (Kaby Lake), 1 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.300
  [Host]             : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  ShortRun-.NET 10.0 : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3

Job=ShortRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=3  
LaunchCount=1  WarmupCount=3  

 Method                    | LineCount | Mean        | Error        | StdDev    | Gen0   | Allocated |
-------------------------- |---------- |------------:|-------------:|----------:|-------:|----------:|
 **Calculate**                 | **1**         |    **270.7 ns** |     **28.65 ns** |   **1.57 ns** | **0.1884** |     **592 B** |
 SerializeResultToUtf8Json | 1         |    873.1 ns |    179.97 ns |   9.86 ns | 0.1936 |     608 B |
 **Calculate**                 | **10**        |  **2,684.1 ns** |    **196.80 ns** |  **10.79 ns** | **0.6256** |    **1968 B** |
 SerializeResultToUtf8Json | 10        |  3,067.4 ns |    206.63 ns |  11.33 ns | 0.4883 |    1536 B |
 **Calculate**                 | **100**       | **25,562.8 ns** |  **5,635.07 ns** | **308.88 ns** | **4.5776** |   **14424 B** |
 SerializeResultToUtf8Json | 100       | 26,132.5 ns | 11,498.90 ns | 630.29 ns | 3.3875 |   10721 B |
