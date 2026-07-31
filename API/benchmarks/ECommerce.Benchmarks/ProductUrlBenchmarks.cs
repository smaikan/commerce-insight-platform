using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ECommerce.Application.Common.Services;

namespace ECommerce.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
public class ProductUrlBenchmarks
{
    private readonly ProductUrlGenerator _generator = new();

    [Params(
        "Basic Cotton T-Shirt",
        "Turkce Sapkali Premium Urun 2026",
        "Premium Wireless Noise Cancelling Headphones with Carrying Case and USB-C Cable")]
    public string Title { get; set; } = string.Empty;

    // Burada farklı uzunluktaki ürün başlıklarının URL'ye dönüştürülmesini ölçüyorum.
    [Benchmark]
    public string Generate() => _generator.Generate(Title);
}
