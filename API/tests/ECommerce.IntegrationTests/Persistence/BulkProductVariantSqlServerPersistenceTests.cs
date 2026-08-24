using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Products.Variants.Commands.BulkUpdateProductVariants;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using ECommerce.Persistence.Services;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class BulkProductVariantSqlServerPersistenceTests
{
    // Burada SQL Server unique index altında ikili takas, üçlü döngü, rollback, stok, seçenek ve concurrency kurallarını doğruluyorum.
    [SqlServerFact]
    public async Task BulkUpdate_Should_Atomically_Swap_And_Cycle_Skus()
    {
        var databaseName = $"ECommerceVariantBulkTests_{Guid.NewGuid():N}";
        var options = CreateOptions(databaseName);

        try
        {
            var seed = await SeedVariantsAsync(options);

            var swapResult = await ExecuteAsync(options, new BulkUpdateProductVariantsCommand(seed.ProductId,
            [
                CreateItem(seed.First, "SKU-B", "Uzunluk", "45 CM", stock: 5),
                CreateItem(seed.Second, "SKU-A", "Uzunluk", "50 CM", stock: 3)
            ]));

            swapResult.Select(variant => variant.Sku).Should().Equal("SKU-B", "SKU-A");
            await AssertSkusAsync(options, (seed.First.Id, "SKU-B"), (seed.Second.Id, "SKU-A"));

            var cycleResult = await ExecuteAsync(options, new BulkUpdateProductVariantsCommand(seed.ProductId,
            [
                CreateItem(swapResult[0], "SKU-A", "Renk / Beden", "Kırmızı / M", stock: 5),
                CreateItem(swapResult[1], "SKU-C", "Renk / Beden", "Kahverengi / L", stock: 3),
                CreateItem(seed.Third with { ConcurrencyToken = await GetTokenAsync(options, seed.Third.Id) }, "SKU-B", "Renk / Beden", "Siyah / XL", stock: 0)
            ]));

            cycleResult.Select(variant => variant.Sku).Should().Equal("SKU-A", "SKU-C", "SKU-B");
            await AssertSkusAsync(
                options,
                (seed.First.Id, "SKU-A"),
                (seed.Second.Id, "SKU-C"),
                (seed.Third.Id, "SKU-B"));
            await AssertOptionLinksAsync(options, seed.First.Id, ("Renk", "Kırmızı", 0), ("Beden", "M", 1));
            await AssertSingleStockAdjustmentAsync(options, seed.First.Id, 3);
            await AssertNoTemporarySkuAsync(options);

            await AssertExternalConflictRollsBackAsync(options, seed, cycleResult);
            await AssertSecondSaveFailureRollsBackAsync(options, seed, cycleResult);
            await AssertStaleTokenRollsBackAsync(options, seed, cycleResult);
        }
        finally
        {
            await using var cleanupContext = new AppDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    // Burada test ürününü üç batch varyantı ve bir batch dışı SKU sahibiyle SQL Server'a kaydediyorum.
    private static async Task<VariantSeed> SeedVariantsAsync(DbContextOptions<AppDbContext> options)
    {
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var product = new Product("Kolye", "kolye", "KOLYE-MAIN");
        var first = new ProductVariant(product, "Uzunluk", "SKU-A", 100m, 2, value: "45 CM");
        var second = new ProductVariant(product, "Uzunluk", "SKU-B", 100m, 3, value: "50 CM");
        var third = new ProductVariant(product, "Uzunluk", "SKU-C", 100m, 0, value: "55 CM");
        var outside = new ProductVariant(product, "Uzunluk", "SKU-OUTSIDE", 100m, 0, value: "60 CM");
        product.Variants.Add(first);
        product.Variants.Add(second);
        product.Variants.Add(third);
        product.Variants.Add(outside);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        return new VariantSeed(
            product.Id,
            first.ToDto(),
            second.ToDto(),
            third.ToDto(),
            outside.ToDto());
    }

    // Burada gerçek repository, resolver ve UnitOfWork ile batch handler'ı çalıştırıyorum.
    private static async Task<IReadOnlyList<ProductVariantDto>> ExecuteAsync(
        DbContextOptions<AppDbContext> options,
        BulkUpdateProductVariantsCommand command)
    {
        await using var context = new AppDbContext(options);
        var handler = new BulkUpdateProductVariantsCommandHandler(
            new ProductVariantRepository(context),
            new VariantOptionResolver(context),
            new UnitOfWork(context));
        return await handler.Handle(command, CancellationToken.None);
    }

    // Burada batch satırını mevcut DTO tokenı ve hedef değerlerle oluşturuyorum.
    private static BulkUpdateProductVariantItem CreateItem(
        ProductVariantDto variant,
        string sku,
        string name,
        string value,
        int stock)
    {
        return new BulkUpdateProductVariantItem(
            variant.Id,
            name,
            value,
            sku,
            100m,
            stock,
            variant.ConcurrencyToken,
            StockAdjustmentReason: "Verified batch count");
    }

    // Burada batch dışındaki SKU çakışmasının bütün hedef değerleri değiştirmeden bıraktığını doğruluyorum.
    private static async Task AssertExternalConflictRollsBackAsync(
        DbContextOptions<AppDbContext> options,
        VariantSeed seed,
        IReadOnlyList<ProductVariantDto> current)
    {
        var command = new BulkUpdateProductVariantsCommand(seed.ProductId,
        [
            CreateItem(current[0], "SKU-OUTSIDE", "Renk", "Kırmızı", 5),
            CreateItem(current[1], "SKU-ROLLBACK", "Renk", "Kahverengi", 3)
        ]);
        var action = () => ExecuteAsync(options, command);

        var exception = await action.Should().ThrowAsync<ProductVariantSkuConflictException>();
        exception.Which.ErrorCode.Should().Be("product_variant_sku_conflict");
        exception.Which.Errors.Should().ContainKey("variants[0].sku");
        await AssertSkusAsync(options, (seed.First.Id, "SKU-A"), (seed.Second.Id, "SKU-C"));
        await AssertNoTemporarySkuAsync(options);
    }

    // Burada ara SKU kaydı yapıldıktan sonraki yapay persistence hatasının transactionı tamamen geri aldığını doğruluyorum.
    private static async Task AssertSecondSaveFailureRollsBackAsync(
        DbContextOptions<AppDbContext> options,
        VariantSeed seed,
        IReadOnlyList<ProductVariantDto> current)
    {
        await using var context = new AppDbContext(options);
        var failingUnitOfWork = new FailOnSecondSaveUnitOfWork(context);
        var handler = new BulkUpdateProductVariantsCommandHandler(
            new ProductVariantRepository(context),
            new VariantOptionResolver(context),
            failingUnitOfWork);
        var command = new BulkUpdateProductVariantsCommand(seed.ProductId,
        [
            CreateItem(current[0], "SKU-ROLL-A", "Renk", "Kırmızı", 5),
            CreateItem(current[1], "SKU-ROLL-B", "Renk", "Kahverengi", 3)
        ]);
        var action = () => handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Injected second-save failure.");
        await AssertSkusAsync(options, (seed.First.Id, "SKU-A"), (seed.Second.Id, "SKU-C"));
        await AssertNoTemporarySkuAsync(options);
    }

    // Burada tek stale tokenın hiçbir varyantı değiştirmeden concurrency_conflict yoluna girdiğini doğruluyorum.
    private static async Task AssertStaleTokenRollsBackAsync(
        DbContextOptions<AppDbContext> options,
        VariantSeed seed,
        IReadOnlyList<ProductVariantDto> current)
    {
        var command = new BulkUpdateProductVariantsCommand(seed.ProductId,
        [
            CreateItem(current[0] with { ConcurrencyToken = Guid.NewGuid() }, "SKU-STALE-A", "Renk", "Kırmızı", 5),
            CreateItem(current[1], "SKU-STALE-B", "Renk", "Kahverengi", 3)
        ]);
        var action = () => ExecuteAsync(options, command);

        await action.Should().ThrowAsync<ConcurrencyException>();
        await AssertSkusAsync(options, (seed.First.Id, "SKU-A"), (seed.Second.Id, "SKU-C"));
        await AssertNoTemporarySkuAsync(options);
    }

    // Burada seçili varyantların kalıcı SKU değerlerini kimlik sırasından bağımsız karşılaştırıyorum.
    private static async Task AssertSkusAsync(
        DbContextOptions<AppDbContext> options,
        params (Guid Id, string Sku)[] expected)
    {
        await using var context = new AppDbContext(options);
        var ids = expected.Select(item => item.Id).ToArray();
        var stored = await context.ProductVariants
            .AsNoTracking()
            .Where(variant => ids.Contains(variant.Id))
            .ToDictionaryAsync(variant => variant.Id, variant => variant.Sku);
        stored.Should().BeEquivalentTo(expected.ToDictionary(item => item.Id, item => item.Sku));
    }

    // Burada batch sonrasında hiçbir geçici SKU değerinin veritabanında kalmadığını doğruluyorum.
    private static async Task AssertNoTemporarySkuAsync(DbContextOptions<AppDbContext> options)
    {
        await using var context = new AppDbContext(options);
        var exists = await context.ProductVariants
            .AsNoTracking()
            .AnyAsync(variant => variant.Sku.StartsWith("__BULK__"));
        exists.Should().BeFalse();
    }

    // Burada birleşik seçenek bağlantılarının doğru ad, değer ve sıra ile kaydedildiğini doğruluyorum.
    private static async Task AssertOptionLinksAsync(
        DbContextOptions<AppDbContext> options,
        Guid variantId,
        params (string Name, string Value, int DisplayOrder)[] expected)
    {
        await using var context = new AppDbContext(options);
        var stored = await context.ProductVariantOptionValues
            .AsNoTracking()
            .Where(item => item.ProductVariantId == variantId)
            .Include(item => item.VariantOptionValue)
                .ThenInclude(item => item.VariantOptionName)
            .OrderBy(item => item.DisplayOrder)
            .Select(item => new
            {
                Name = item.VariantOptionValue.VariantOptionName.Name,
                item.VariantOptionValue.Value,
                item.DisplayOrder
            })
            .ToListAsync();
        stored.Select(item => (item.Name, item.Value, item.DisplayOrder)).Should().Equal(expected);
    }

    // Burada hedef stok farkı için tam bir StockCountAdjustment hareketi oluştuğunu doğruluyorum.
    private static async Task AssertSingleStockAdjustmentAsync(
        DbContextOptions<AppDbContext> options,
        Guid variantId,
        int expectedDelta)
    {
        await using var context = new AppDbContext(options);
        var movements = await context.StockMovements
            .AsNoTracking()
            .Where(movement =>
                movement.ProductVariantId == variantId &&
                movement.Type == StockMovementType.StockCountAdjustment)
            .ToListAsync();
        movements.Should().ContainSingle(movement =>
            movement.QuantityDelta == expectedDelta &&
            movement.Reason == "Verified batch count");
    }

    // Burada güncel batch isteğinde kullanılacak kalıcı concurrency tokenını okuyorum.
    private static async Task<Guid> GetTokenAsync(
        DbContextOptions<AppDbContext> options,
        Guid variantId)
    {
        await using var context = new AppDbContext(options);
        return await context.ProductVariants
            .AsNoTracking()
            .Where(variant => variant.Id == variantId)
            .Select(variant => variant.ConcurrencyToken)
            .SingleAsync();
    }

    // Burada yalnız açıkça yapılandırılmış SQL Server test ortamına benzersiz veritabanı seçenekleri üretiyorum.
    private static DbContextOptions<AppDbContext> CreateOptions(string databaseName)
    {
        var server = Environment.GetEnvironmentVariable("ECOMMERCE_TEST_SQL_SERVER");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
        var connectionString = OperatingSystem.IsWindows() &&
                               (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(password))
            ? $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;"
            : new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = databaseName,
                UserID = "sa",
                Password = password,
                TrustServerCertificate = true,
                MultipleActiveResultSets = true
            }.ConnectionString;

        return new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;
    }

    private sealed record VariantSeed(
        long ProductId,
        ProductVariantDto First,
        ProductVariantDto Second,
        ProductVariantDto Third,
        ProductVariantDto Outside);

    private sealed class FailOnSecondSaveUnitOfWork : IUnitOfWork
    {
        private readonly UnitOfWork _inner;
        private int _saveCount;

        // Burada ikinci SaveChanges çağrısında transaction rollback'ini sınamak için sarmalayıcıyı hazırlıyorum.
        public FailOnSecondSaveUnitOfWork(AppDbContext context)
        {
            _inner = new UnitOfWork(context);
        }

        // Burada ilk ara SKU kaydını yapıp ikinci nihai kayıt çağrısında kontrollü hata üretiyorum.
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (++_saveCount == 2)
            {
                throw new InvalidOperationException("Injected second-save failure.");
            }

            return _inner.SaveChangesAsync(cancellationToken);
        }

        // Burada yapay SaveChanges hatasını gerçek serializable transaction sınırı içinde çalıştırıyorum.
        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            return _inner.ExecuteInSerializableTransactionAsync(operation, cancellationToken);
        }
    }
}
