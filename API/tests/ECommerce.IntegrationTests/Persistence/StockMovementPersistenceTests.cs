using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class StockMovementPersistenceTests
{
    // Burada imzalı hareketin yeni satır olarak kaydedilip hızlı stok bakiyesiyle aynı toplamı verdiğini doğruluyorum.
    [Fact]
    public async Task Repository_Should_List_Signed_Movement_And_Reconcile_Current_Balance()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        Guid variantId;

        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            var product = new Product("Ledger Product", "ledger-product", "LEDGER-MAIN");
            var variant = new ProductVariant(
                product,
                "Standard",
                "LEDGER-STANDARD",
                100m,
                5);
            product.Variants.Add(variant);
            seedContext.Products.Add(product);
            await seedContext.SaveChangesAsync();
            variantId = variant.Id;
        }

        await using (var writeContext = new AppDbContext(options))
        {
            var variant = await writeContext.ProductVariants
                .SingleAsync(item => item.Id == variantId);
            variant.ApplyStockMovement(
                -2,
                StockMovementType.Damage,
                "Damaged during warehouse handling.");
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new AppDbContext(options);
        var repository = new StockMovementRepository(readContext);

        var movements = await repository.GetListAsync(new StockMovementListFilter(
            PageNumber: 1,
            PageSize: 20,
            ProductVariantId: variantId,
            Direction: StockMovementDirection.Out,
            Type: StockMovementType.Damage));
        var balance = await repository.GetBalanceAsync(variantId);

        movements.Items.Should().ContainSingle();
        movements.TotalCount.Should().Be(1);
        movements.Items.Single().QuantityDelta.Should().Be(-2);
        movements.Items.Single().StockBeforeMovement.Should().Be(5);
        movements.Items.Single().StockAfterMovement.Should().Be(3);
        balance.Should().NotBeNull();
        balance!.PersistedStock.Should().Be(3);
        balance.MovementBalance.Should().Be(3);
    }

    // Burada ledger tablosunun yön, denklem, referans, idempotency ve audit silme kurallarını EF modelinde doğruluyorum.
    [Fact]
    public void Model_Should_Configure_Stock_Movement_Integrity_And_Idempotency_Rules()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var options = CreateOptions(connection);
        using var context = new AppDbContext(options);
        var designTimeModel = context.GetService<IDesignTimeModel>().Model;
        var movementEntity = designTimeModel.FindEntityType(typeof(StockMovement));

        movementEntity.Should().NotBeNull();
        var constraintNames = movementEntity!.GetCheckConstraints()
            .Select(constraint => constraint.Name)
            .ToHashSet();
        constraintNames.Should().Contain(new[]
        {
            "CK_StockMovements_QuantityDelta_NonZero",
            "CK_StockMovements_Direction_Matches_Delta",
            "CK_StockMovements_Stock_Equation",
            "CK_StockMovements_Type_Matches_Direction",
            "CK_StockMovements_Required_Reference"
        });

        var orderIndex = movementEntity.GetIndexes().Single(index =>
            index.GetDatabaseName() == "UX_StockMovements_OrderId_ProductVariantId_Type");
        var returnIndex = movementEntity.GetIndexes().Single(index =>
            index.GetDatabaseName() == "UX_StockMovements_ReturnRequestId_ProductVariantId_Type");
        orderIndex.IsUnique.Should().BeTrue();
        orderIndex.GetFilter().Should().Be(
            "[OrderId] IS NOT NULL AND [ReturnRequestId] IS NULL AND [Type] IN (20, 60)");
        returnIndex.IsUnique.Should().BeTrue();
        returnIndex.GetFilter().Should().Be(
            "[ReturnRequestId] IS NOT NULL AND [Type] IN (20, 21)");

        var variantForeignKey = movementEntity.GetForeignKeys().Single(foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(ProductVariant));
        variantForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }

    // Burada stok hareketi ilişkisel testleri için açık SQLite bağlantısı oluşturuyorum.
    private static async Task<SqliteConnection> CreateOpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    // Burada stok hareketi test DbContext ayarlarını açık SQLite bağlantısına bağlıyorum.
    private static DbContextOptions<AppDbContext> CreateOptions(SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
    }
}
