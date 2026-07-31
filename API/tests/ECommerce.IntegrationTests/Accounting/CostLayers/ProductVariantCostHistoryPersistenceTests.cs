using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Accounting.Repositories;
using ECommerce.Persistence.Context;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ECommerce.IntegrationTests.Accounting.CostLayers;

public sealed class ProductVariantCostHistoryPersistenceTests
{
    // Burada varyant maliyet geçmişinin gerçek SQLite repository sorgusunda tarih, oluşturulma ve kimlik sırasını koruduğunu doğruluyorum.
    [Fact]
    public async Task Repository_Should_Return_Deterministic_Chronological_History()
    {
        await using var fixture = await HistoryFixture.CreateAsync();
        var histories = fixture.CreateHistoryTimeline();
        fixture.Context.Set<ProductVariantCostHistory>().AddRange(histories);
        await fixture.Context.SaveChangesAsync();
        fixture.Context.ChangeTracker.Clear();
        var repository = new ProductVariantCostHistoryRepository(
            fixture.Context);

        var result = await repository.GetByProductVariantIdAsync(
            fixture.ProductVariantId,
            CancellationToken.None);
        var expectedIds = histories
            .OrderBy(history => history.ValidFrom)
            .ThenBy(history => history.CreatedAt)
            .ThenBy(history => history.Id)
            .Select(history => history.Id);

        result.Select(history => history.Id).Should().Equal(expectedIds);
        result.Should().HaveCount(3);
        result.Should().Contain(history =>
            history.SourceType ==
                ProductVariantCostHistorySourceType.PurchaseInvoice);
        result.Should().Contain(history =>
            history.SourceType ==
                ProductVariantCostHistorySourceType.OpeningBalance);
        result.Should().ContainSingle(history => history.ValidTo == null);
        (await repository.GetByProductVariantIdAsync(
                Guid.NewGuid(),
                CancellationToken.None))
            .Should()
            .BeEmpty();
    }

    // Burada EF modelinin kaynak türünü zorunlu tuttuğunu ve history sorgusu ile kaynak izleme indekslerini içerdiğini doğruluyorum.
    [Fact]
    public async Task Configuration_Should_Contain_Source_And_Deterministic_Query_Constraints()
    {
        await using var fixture = await HistoryFixture.CreateAsync();
        var designTimeModel = fixture.Context
            .GetService<IDesignTimeModel>()
            .Model;
        var entityType = designTimeModel.FindEntityType(
            typeof(ProductVariantCostHistory));

        entityType.Should().NotBeNull();
        entityType!.FindProperty(nameof(ProductVariantCostHistory.SourceType))!
            .IsNullable.Should().BeFalse();
        entityType.GetCheckConstraints()
            .Should()
            .Contain(constraint =>
                constraint.Name ==
                "CK_AccountingProductVariantCostHistory_SourceType");
        entityType.GetIndexes().Should().Contain(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(
            new[]
            {
                nameof(ProductVariantCostHistory.ProductVariantId),
                nameof(ProductVariantCostHistory.ValidFrom),
                nameof(ProductVariantCostHistory.CreatedAt),
                nameof(ProductVariantCostHistory.Id)
            }));
        entityType.GetIndexes().Should().Contain(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(
            new[]
            {
                nameof(ProductVariantCostHistory.SourceType),
                nameof(ProductVariantCostHistory.SourceId)
            }));
        entityType.GetIndexes().Should().Contain(index =>
            index.IsUnique &&
            index.GetFilter() == "[ValidTo] IS NULL" &&
            index.Properties.Select(property => property.Name).SequenceEqual(
            new[]
            {
                nameof(ProductVariantCostHistory.ProductVariantId)
            }));
    }

    // Burada aynı varyanta iki aktif maliyet geçmişi yazılmasının gerçek filtreli unique indeks tarafından reddedildiğini doğruluyorum.
    [Fact]
    public async Task Persistence_Should_Reject_Two_Active_Histories_For_One_Variant()
    {
        await using var fixture = await HistoryFixture.CreateAsync();
        var first = new ProductVariantCostHistory(
            fixture.ProductVariantId,
            null,
            10m,
            null,
            12m,
            new DateTime(2026, 7, 26),
            5,
            Guid.NewGuid(),
            ProductVariantCostHistorySourceType.OpeningBalance);
        var second = new ProductVariantCostHistory(
            fixture.ProductVariantId,
            10m,
            11m,
            12m,
            13.2m,
            new DateTime(2026, 7, 27),
            5,
            Guid.NewGuid(),
            ProductVariantCostHistorySourceType.PurchaseInvoice);
        fixture.Context.Set<ProductVariantCostHistory>().AddRange(first, second);

        Func<Task> save = () => fixture.Context.SaveChangesAsync();

        await save.Should().ThrowAsync<DbUpdateException>();
    }

    private sealed class HistoryFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public AppDbContext Context { get; }
        public Guid ProductVariantId { get; }

        // Burada SQLite maliyet geçmişi fixture'ının bağlantı, context ve varyant kimliğini saklıyorum.
        private HistoryFixture(
            SqliteConnection connection,
            AppDbContext context,
            Guid productVariantId)
        {
            _connection = connection;
            Context = context;
            ProductVariantId = productVariantId;
        }

        // Burada gerçek ilişkisel model ve bağlı ProductVariant kaydıyla maliyet geçmişi fixture'ı oluşturuyorum.
        public static async Task<HistoryFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();
            var product = new Product(
                "History Product",
                "history-product",
                $"HISTORY-{Guid.NewGuid():N}");
            var variant = new ProductVariant(
                product,
                "Default",
                $"HISTORY-V-{Guid.NewGuid():N}",
                100m,
                0);
            product.Variants.Add(variant);
            context.Products.Add(product);
            await context.SaveChangesAsync();
            return new HistoryFixture(connection, context, variant.Id);
        }

        // Burada repository sırasını sınamak için karışık eklenen ve aynı başlangıç tarihini paylaşan history kayıtlarını hazırlıyorum.
        public IReadOnlyList<ProductVariantCostHistory> CreateHistoryTimeline()
        {
            var laterClosed = new ProductVariantCostHistory(
                ProductVariantId,
                8m,
                10m,
                9.6m,
                12m,
                new DateTime(2026, 7, 28),
                5,
                Guid.NewGuid(),
                ProductVariantCostHistorySourceType.PurchaseInvoice);
            laterClosed.Close(new DateTime(2026, 7, 29), 4);
            var earliest = new ProductVariantCostHistory(
                ProductVariantId,
                null,
                8m,
                null,
                9.6m,
                new DateTime(2026, 7, 26),
                6,
                Guid.NewGuid(),
                ProductVariantCostHistorySourceType.OpeningBalance);
            earliest.Close(new DateTime(2026, 7, 28), 5);
            var active = new ProductVariantCostHistory(
                ProductVariantId,
                10m,
                11m,
                12m,
                13.2m,
                new DateTime(2026, 7, 28),
                4,
                Guid.NewGuid(),
                ProductVariantCostHistorySourceType.PurchaseInvoice);
            return [laterClosed, earliest, active];
        }

        // Burada SQLite fixture kaynaklarını test sonunda serbest bırakıyorum.
        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
