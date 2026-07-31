using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ECommerce.Application.Common.Identifiers;

namespace ECommerce.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
public class IdentifierBenchmarks
{
    private const long ProductId = 1_234_567_890;
    private readonly string _encodedProductId = PublicIdCodec.EncodeProductId(ProductId);

    // Burada ürün kimliğinin public kimliğe dönüştürülme maliyetini ölçüyorum.
    [Benchmark]
    public string EncodeProductId() => PublicIdCodec.EncodeProductId(ProductId);

    // Burada public ürün kimliğinin doğrulanıp çözülme maliyetini ölçüyorum.
    [Benchmark]
    public bool DecodeProductId() =>
        PublicIdCodec.TryDecodeProductId(_encodedProductId, out _);
}
