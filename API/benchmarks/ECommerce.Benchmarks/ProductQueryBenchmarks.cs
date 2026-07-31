using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
public class ProductQueryBenchmarks
{
    private DbContextOptions<AppDbContext> _options = null!;

    // Burada katalog sorgusu için her çalıştırmada aynı büyüklükte veri kümesini hazırlıyorum.
    [GlobalSetup]
    public async Task SetupAsync()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"product-query-benchmark-{Guid.NewGuid():N}")
            .Options;

        await using var context = new AppDbContext(_options);
        var products = Enumerable.Range(1, 1_000)
            .Select(index => new Product(
                $"Performance Product {index:0000}",
                $"performance-product-{index:0000}",
                $"PERF-{index:0000}",
                status: ProductStatus.Active,
                displayOrder: index))
            .ToArray();
        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }

    // Burada mevcut varsayılan ürün listeleme sorgusunun ilk sayfa maliyetini ölçüyorum.
    [Benchmark(Baseline = true)]
    public Task<PagedResult<Product>> EntityGraphFirstPage() =>
        QueryAsync(new ProductListFilter(1, 20));

    // Burada mevcut metin aramalı ürün listeleme sorgusunun maliyetini ölçüyorum.
    [Benchmark]
    public Task<PagedResult<Product>> EntityGraphSearchPage() =>
        QueryAsync(new ProductListFilter(1, 20, Search: "Product 099"));

    // Burada endpoint'in kullandığı doğrudan DTO projeksiyonunun ilk sayfa maliyetini ölçüyorum.
    [Benchmark]
    public Task<PagedResult<ProductDto>> ProjectedFirstPage() =>
        ProjectedQueryAsync(new ProductListFilter(1, 20));

    // Burada endpoint'in kullandığı doğrudan DTO projeksiyonunun arama maliyetini ölçüyorum.
    [Benchmark]
    public Task<PagedResult<ProductDto>> ProjectedSearchPage() =>
        ProjectedQueryAsync(new ProductListFilter(1, 20, Search: "Product 099"));

    // Burada her tekrar için yeni DbContext ile gerçek repository yolunu çalıştırıyorum.
    private async Task<PagedResult<Product>> QueryAsync(ProductListFilter filter)
    {
        await using var context = new AppDbContext(_options);
        var repository = new ProductRepository(context);
        return await repository.GetListAsync(filter);
    }

    // Burada her tekrar için yeni DbContext ile endpoint'in kullandığı projection yolunu çalıştırıyorum.
    private async Task<PagedResult<ProductDto>> ProjectedQueryAsync(ProductListFilter filter)
    {
        await using var context = new AppDbContext(_options);
        var reader = new ProductListReader(context);
        return await reader.GetListAsync(filter);
    }
}
