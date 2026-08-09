using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
public class OrderListQueryBenchmarks
{
    private DbContextOptions<AppDbContext> _options = null!;

    // Burada sipariş listesi için aynı büyüklükte ve aranabilir müşteri snapshot'lı veri kümesini hazırlıyorum.
    [GlobalSetup]
    public async Task SetupAsync()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"order-list-benchmark-{Guid.NewGuid():N}")
            .Options;

        await using var context = new AppDbContext(_options);
        var orders = Enumerable.Range(1, 1_000)
            .Select(CreateOrder)
            .ToArray();
        await context.Orders.AddRangeAsync(orders);
        await context.SaveChangesAsync();
    }

    // Burada yönetici sipariş listesinin varsayılan ilk sayfasını ölçüyorum.
    [Benchmark(Baseline = true)]
    public Task<PagedResult<OrderSummaryDto>> FirstPage() =>
        QueryAsync(new OrderListFilter(1, 20));

    // Burada müşteri adı ve sipariş numarasını kapsayan serbest metin aramasını ölçüyorum.
    [Benchmark]
    public Task<PagedResult<OrderSummaryDto>> SearchPage() =>
        QueryAsync(new OrderListFilter(1, 20, Search: "Müşteri 099"));

    // Burada uzak sayfada Skip ve toplam sayım maliyetini ayrı senaryoda görünür kılıyorum.
    [Benchmark]
    public Task<PagedResult<OrderSummaryDto>> DeepPage() =>
        QueryAsync(new OrderListFilter(40, 20));

    // Burada her ölçümde yeni DbContext ile gerçek liste projection yolunu çalıştırıyorum.
    private async Task<PagedResult<OrderSummaryDto>> QueryAsync(OrderListFilter filter)
    {
        await using var context = new AppDbContext(_options);
        var reader = new OrderListReader(context);
        return await reader.GetListAsync(filter);
    }

    // Burada valid aggregate kurallarını koruyan, sıralanabilir ve aranabilir sipariş test verisini üretiyorum.
    private static Order CreateOrder(int index)
    {
        var order = new Order(
            userId: index,
            orderNumber: $"ORD-{index:000000}",
            subTotal: 100m,
            discountTotal: 0m,
            shippingTotal: 0m,
            taxTotal: 0m,
            grandTotal: 100m);
        order.SetCustomerSnapshot($"Müşteri {index:0000}", "Test", $"customer-{index:0000}@example.test", "5550000000");
        order.AddItem(index, Guid.NewGuid(), "Benchmark ürünü", $"BENCH-{index:0000}", 100m, 1);
        order.SetImportedCreatedAt(DateTime.UtcNow.AddMinutes(-index));
        return order;
    }
}
