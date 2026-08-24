using ECommerce.Application.StockMovements.Commands.BulkCreateStockMovements;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class StockMovementSkuPersistenceTests
{
    // Burada toplu stok hareketinin gerçek repository ile SKU üzerinden eşleşip atomik kaydedildiğini doğruluyorum.
    [Fact]
    public async Task Bulk_Stock_Movement_Should_Resolve_Active_Variants_By_Sku()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            var product = new Product("SKU stock", "sku-stock", "SKU-STOCK-MAIN");
            product.Variants.Add(new ProductVariant(product, "Small", "SKU-STOCK-S", 100m, 3));
            product.Variants.Add(new ProductVariant(product, "Large", "SKU-STOCK-L", 120m, 2));
            seedContext.Products.Add(product);
            await seedContext.SaveChangesAsync();
        }

        await using (var mutationContext = new AppDbContext(options))
        {
            var handler = new BulkCreateStockMovementsCommandHandler(
                new ProductVariantRepository(mutationContext),
                new UnitOfWork(mutationContext));

            var result = await handler.Handle(
                new BulkCreateStockMovementsCommand(
                [
                    new BulkStockMovementItem("  SKU-STOCK-S  ", 4, StockMovementType.Purchase, "Mal kabul"),
                    new BulkStockMovementItem("SKU-STOCK-L", -1, StockMovementType.Damage, "Hasar")
                ]),
                CancellationToken.None);

            result.MovementCount.Should().Be(2);
        }

        await using var readContext = new AppDbContext(options);
        var balances = await readContext.ProductVariants
            .AsNoTracking()
            .OrderBy(variant => variant.Sku)
            .ToDictionaryAsync(variant => variant.Sku, variant => variant.Stock);
        balances["SKU-STOCK-L"].Should().Be(1);
        balances["SKU-STOCK-S"].Should().Be(7);
        (await readContext.StockMovements.CountAsync()).Should().Be(4);
    }
}
