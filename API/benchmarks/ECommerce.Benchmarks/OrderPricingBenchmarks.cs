using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ECommerce.Application.Orders.Services;

namespace ECommerce.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
public class OrderPricingBenchmarks
{
    private readonly OrderPricingService _service = new();
    private IReadOnlyCollection<OrderPricingLine> _lines = null!;
    private OrderPricingResult _result = null!;

    [Params(1, 10, 100)]
    public int LineCount { get; set; }

    // Burada her ölçüm grubu için kararlı fiyatlandırma girdisini hazırlıyorum.
    [GlobalSetup]
    public void Setup()
    {
        _lines = Enumerable.Range(1, LineCount)
            .Select(index => new OrderPricingLine(
                CreateDeterministicGuid(index),
                99.90m + index,
                index % 2 == 0 ? 20m : 10m))
            .ToArray();
        _result = _service.Calculate(_lines, 10m, 49.90m);
    }

    // Burada sipariş satırlarının indirim, vergi ve toplam hesaplama maliyetini ölçüyorum.
    [Benchmark]
    public OrderPricingResult Calculate() =>
        _service.Calculate(_lines, 10m, 49.90m);

    // Burada hesaplanan sipariş sonucunun UTF-8 JSON üretim maliyetini ölçüyorum.
    [Benchmark]
    public byte[] SerializeResultToUtf8Json() =>
        JsonSerializer.SerializeToUtf8Bytes(_result);

    // Burada sıralama sonucunu tekrarlanabilir kılan sabit GUID değerini üretiyorum.
    private static Guid CreateDeterministicGuid(int value)
    {
        Span<byte> bytes = stackalloc byte[16];
        BitConverter.TryWriteBytes(bytes, value);
        return new Guid(bytes);
    }
}
