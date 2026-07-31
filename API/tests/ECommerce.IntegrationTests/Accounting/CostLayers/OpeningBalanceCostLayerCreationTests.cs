using ECommerce.Application.Accounting.CostLayers;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Services;
using ECommerce.Application.Products.Commands.BulkCreateProducts;
using ECommerce.Application.Products.Commands.CreateProduct;
using ECommerce.Application.Products.Variants.Commands.CreateProductVariant;
using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Accounting.Repositories;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Accounting.CostLayers;

public sealed class OpeningBalanceCostLayerCreationTests
{
    // Burada CreateProduct akışının opsiyonel maliyeti aynı UoW'deki tek katmana aktardığını ve sıfır stoklu varyantı atladığını doğruluyorum.
    [Fact]
    public async Task CreateProduct_Should_Persist_Optional_Cost_Only_For_Positive_Opening_Stock()
    {
        await using var fixture = await OpeningLayerFixture.CreateAsync();
        var handler = fixture.CreateProductHandler();

        var result = await handler.Handle(
            new CreateProductCommand(
                "Opening Product",
                "OPENING-MAIN",
                Variants:
                [
                    new CreateProductVariantItem(
                        "With Stock",
                        "OPENING-STOCK",
                        100m,
                        4,
                        OpeningUnitCostExcludingVat: 12.3456m),
                    new CreateProductVariantItem(
                        "Without Stock",
                        "OPENING-ZERO",
                        100m,
                        0)
                ]),
            CancellationToken.None);

        var layer = await fixture.Context
            .Set<InventoryCostLayer>()
            .SingleAsync();
        var stockedVariant = result.Variants.Single(item => item.Stock == 4);
        var openingMovement = await fixture.Context.StockMovements
            .SingleAsync(item =>
                item.ProductVariantId == stockedVariant.Id &&
                item.Type == StockMovementType.OpeningBalance);
        layer.SourceType.Should().Be(
            InventoryCostLayerSourceType.OpeningBalance);
        layer.StockMovementId.Should().Be(openingMovement.Id);
        layer.OriginalQuantity.Should().Be(4);
        layer.RemainingQuantity.Should().Be(4);
        layer.UnitCostExcludingVat.Should().Be(12.3456m);
        layer.UnitCostIncludingVat.Should().Be(12.3456m);
        layer.TotalCostExcludingVat.Should().Be(49.38m);
        stockedVariant.Price.Should().Be(100m);
        var history = await fixture.Context
            .Set<ProductVariantCostHistory>()
            .SingleAsync();
        history.SourceType.Should().Be(
            ProductVariantCostHistorySourceType.OpeningBalance);
        history.SourceId.Should().Be(layer.Id);
        history.NewCostExcludingVat.Should().Be(12.3456m);
        (await fixture.Context.Set<InventoryCostLayer>().CountAsync())
            .Should()
            .Be(1);
    }

    // Burada CreateProductVariant akışının yeni OpeningBalance hareketi ve verilen maliyet katmanını tek SaveChanges ile kalıcılaştırdığını doğruluyorum.
    [Fact]
    public async Task CreateProductVariant_Should_Persist_Opening_Movement_And_Layer_Together()
    {
        await using var fixture = await OpeningLayerFixture.CreateAsync();
        await fixture.CreateProductHandler().Handle(
            new CreateProductCommand(
                "Variant Parent",
                "VARIANT-PARENT",
                Variants:
                [
                    new CreateProductVariantItem(
                        "Initial Zero",
                        "VARIANT-ZERO",
                        100m,
                        0)
                ]),
            CancellationToken.None);
        var productId = await fixture.Context.Products
            .Where(item => item.MainSku == "VARIANT-PARENT")
            .Select(item => item.Id)
            .SingleAsync();

        var result = await fixture.CreateVariantHandler().Handle(
            new CreateProductVariantCommand(
                productId,
                "Opening Variant",
                "VARIANT-OPENING",
                150m,
                3,
                OpeningUnitCostExcludingVat: 20m,
                OpeningUnitCostIncludingVat: 24m),
            CancellationToken.None);

        var movement = await fixture.Context.StockMovements
            .SingleAsync(item =>
                item.ProductVariantId == result.Id &&
                item.Type == StockMovementType.OpeningBalance);
        var layer = await fixture.Context.Set<InventoryCostLayer>()
            .SingleAsync();
        layer.StockMovementId.Should().Be(movement.Id);
        layer.ProductVariantId.Should().Be(result.Id);
        layer.OriginalQuantity.Should().Be(3);
        layer.UnitCostExcludingVat.Should().Be(20m);
        layer.UnitCostIncludingVat.Should().Be(24m);
        result.Price.Should().Be(150m);
        var history = await fixture.Context
            .Set<ProductVariantCostHistory>()
            .SingleAsync();
        history.SourceType.Should().Be(
            ProductVariantCostHistorySourceType.OpeningBalance);
        history.SourceId.Should().Be(layer.Id);
        layer.PurchaseInvoiceLineId.Should().BeNull();
        layer.PurchaseInvoiceStockAllocationId.Should().BeNull();
    }

    // Burada BulkCreateProducts akışının varyant maliyetlerini eşleyip yalnız pozitif açılış stoklarına katman ürettiğini doğruluyorum.
    [Fact]
    public async Task BulkCreateProducts_Should_Persist_Layers_Only_For_Positive_Opening_Stocks()
    {
        await using var fixture = await OpeningLayerFixture.CreateAsync();

        await fixture.CreateBulkProductHandler().Handle(
            new BulkCreateProductsCommand(
            [
                new BulkCreateProductItem(
                    "Bulk Stocked",
                    "BULK-STOCKED",
                    Variants:
                    [
                        new BulkCreateProductVariantItem(
                            "Default",
                            "BULK-STOCKED-V",
                            100m,
                            6,
                            OpeningUnitCostExcludingVat: 8.5m,
                            OpeningUnitCostIncludingVat: 10.2m)
                    ]),
                new BulkCreateProductItem(
                    "Bulk Zero",
                    "BULK-ZERO",
                    Variants:
                    [
                        new BulkCreateProductVariantItem(
                            "Default",
                            "BULK-ZERO-V",
                            100m,
                            0)
                    ])
            ]),
            CancellationToken.None);

        var layers = await fixture.Context
            .Set<InventoryCostLayer>()
            .ToListAsync();
        layers.Should().ContainSingle();
        layers[0].OriginalQuantity.Should().Be(6);
        layers[0].UnitCostExcludingVat.Should().Be(8.5m);
        layers[0].UnitCostIncludingVat.Should().Be(10.2m);
        layers[0].TotalCostExcludingVat.Should().Be(51m);
        var history = await fixture.Context
            .Set<ProductVariantCostHistory>()
            .SingleAsync();
        history.SourceType.Should().Be(
            ProductVariantCostHistorySourceType.OpeningBalance);
        history.NewCostExcludingVat.Should().Be(8.5m);
        (await fixture.Context.StockMovements.CountAsync(item =>
                item.Type == StockMovementType.OpeningBalance))
            .Should()
            .Be(1);
    }

    // Burada OpeningBalance maliyet komutunun token'ı yenilediğini ve eski token ile ikinci güncellemeyi reddettiğini doğruluyorum.
    [Fact]
    public async Task UpdateOpeningCost_Should_Protect_Concurrent_Remaining_Cost_Changes()
    {
        await using var fixture = await OpeningLayerFixture.CreateAsync();
        await fixture.CreateProductHandler().Handle(
            new CreateProductCommand(
                "Revalue Product",
                "REVALUE-MAIN",
                Variants:
                [
                    new CreateProductVariantItem(
                        "Default",
                        "REVALUE-V",
                        100m,
                        5)
                ]),
            CancellationToken.None);
        var layer = await fixture.Context.Set<InventoryCostLayer>()
            .AsNoTracking()
            .SingleAsync();
        var handlers = fixture.CreateOpeningLayerHandlers();

        var updated = await handlers.Handle(
            new UpdateOpeningBalanceCostLayerCommand(
                layer.Id,
                25m,
                null,
                layer.ConcurrencyToken),
            CancellationToken.None);
        Func<Task> staleUpdate = () => handlers.Handle(
            new UpdateOpeningBalanceCostLayerCommand(
                layer.Id,
                40m,
                48m,
                layer.ConcurrencyToken),
            CancellationToken.None);

        updated.UnitCostExcludingVat.Should().Be(25m);
        updated.UnitCostIncludingVat.Should().Be(25m);
        updated.TotalCostExcludingVat.Should().Be(125m);
        updated.ConcurrencyToken.Should().NotBe(layer.ConcurrencyToken);
        await staleUpdate.Should().ThrowAsync<ConcurrencyException>();
        var histories = await fixture.Context
            .Set<ProductVariantCostHistory>()
            .OrderBy(item => item.ValidFrom)
            .ToListAsync();
        histories.Should().HaveCount(2);
        histories.Should().ContainSingle(item =>
            item.ValidTo.HasValue &&
            item.NewCostExcludingVat == 0m);
        histories.Should().ContainSingle(item =>
            !item.ValidTo.HasValue &&
            item.SourceType ==
                ProductVariantCostHistorySourceType.OpeningBalance &&
            item.SourceId == layer.Id &&
            item.PreviousCostExcludingVat == 0m &&
            item.NewCostExcludingVat == 25m);
    }

    private sealed class OpeningLayerFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public AppDbContext Context { get; }
        private ProductRepository ProductRepository { get; }
        private ProductVariantRepository VariantRepository { get; }
        private OpeningBalanceCostLayerWriter LayerWriter { get; }
        private InventoryCostRepository CostRepository { get; }
        private ProductTypeRepository ProductTypeRepository { get; }
        private BrandRepository BrandRepository { get; }
        private TaxRateRepository TaxRateRepository { get; }
        private CollectionRepository CollectionRepository { get; }
        private TagRepository TagRepository { get; }
        private ProductUrlGenerator UrlGenerator { get; }
        private ProductTagResolver TagResolver { get; }
        private UnitOfWork UnitOfWork { get; }

        // Burada gerçek repository ve UoW kullanan in-memory SQLite test sınırını hazırlıyorum.
        private OpeningLayerFixture(
            SqliteConnection connection,
            AppDbContext context)
        {
            _connection = connection;
            Context = context;
            ProductRepository = new ProductRepository(context);
            VariantRepository = new ProductVariantRepository(context);
            var layerRepository =
                new OpeningBalanceCostLayerRepository(context);
            CostRepository = new InventoryCostRepository(context);
            LayerWriter =
                new OpeningBalanceCostLayerWriter(
                    layerRepository,
                    CostRepository);
            ProductTypeRepository = new ProductTypeRepository(context);
            BrandRepository = new BrandRepository(context);
            TaxRateRepository = new TaxRateRepository(context);
            CollectionRepository = new CollectionRepository(context);
            TagRepository = new TagRepository(context);
            UrlGenerator = new ProductUrlGenerator();
            TagResolver = new ProductTagResolver(
                TagRepository,
                UrlGenerator);
            UnitOfWork = new UnitOfWork(context);
        }

        // Burada bütün EF configuration'larını uygulayan temiz SQLite veritabanını oluşturuyorum.
        public static async Task<OpeningLayerFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new OpeningLayerFixture(connection, context);
        }

        // Burada CreateProduct akışını gerçek persistence ve ortak writer ile hazırlıyorum.
        public CreateProductCommandHandler CreateProductHandler()
        {
            return new CreateProductCommandHandler(
                ProductRepository,
                ProductTypeRepository,
                BrandRepository,
                TaxRateRepository,
                CollectionRepository,
                TagResolver,
                UrlGenerator,
                LayerWriter,
                UnitOfWork);
        }

        // Burada tek varyant oluşturma akışını gerçek persistence ve ortak writer ile hazırlıyorum.
        public CreateProductVariantCommandHandler CreateVariantHandler()
        {
            return new CreateProductVariantCommandHandler(
                ProductRepository,
                VariantRepository,
                LayerWriter,
                UnitOfWork);
        }

        // Burada toplu ürün oluşturma akışını gerçek persistence ve ortak writer ile hazırlıyorum.
        public BulkCreateProductsCommandHandler CreateBulkProductHandler()
        {
            return new BulkCreateProductsCommandHandler(
                ProductRepository,
                ProductTypeRepository,
                BrandRepository,
                TaxRateRepository,
                CollectionRepository,
                TagRepository,
                TagResolver,
                UrlGenerator,
                LayerWriter,
                UnitOfWork);
        }

        // Burada OpeningBalance maliyet katmanı CQRS handler'ını gerçek repository ve UoW ile hazırlıyorum.
        public OpeningBalanceCostLayerHandlers CreateOpeningLayerHandlers()
        {
            return new OpeningBalanceCostLayerHandlers(
                new OpeningBalanceCostLayerRepository(Context),
                CostRepository,
                UnitOfWork);
        }

        // Burada SQLite context ve bağlantısını test sonunda serbest bırakıyorum.
        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
