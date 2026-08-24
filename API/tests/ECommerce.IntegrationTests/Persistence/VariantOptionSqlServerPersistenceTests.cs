using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Variants.Commands.UpdateProductVariant;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using ECommerce.Persistence.Services;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class VariantOptionSqlServerPersistenceTests
{
    // Burada seçenek bağlantılarının SQL Server üzerinde INSERT/DELETE olarak izlenip tüm tekli ve birleşik geçişleri kaydettiğini doğruluyorum.
    [SqlServerFact]
    public async Task UpdateProductVariant_Should_Insert_And_Replace_Option_Links()
    {
        var databaseName = $"ECommerceVariantOptionTests_{Guid.NewGuid():N}";
        var options = CreateOptions(databaseName);

        try
        {
            var variantId = await SeedDefaultVariantAsync(options);

            var firstStates = await UpdateVariantAsync(
                options,
                variantId,
                "Uzunluk",
                "40 CM");
            firstStates.Should().ContainSingle(state => state == EntityState.Added);
            await AssertStoredOptionsAsync(options, variantId, [("Uzunluk", "40 CM", 0)]);

            var singleReplacementStates = await UpdateVariantAsync(
                options,
                variantId,
                "Uzunluk",
                "50 CM");
            singleReplacementStates.Should().BeEquivalentTo(
                [EntityState.Deleted, EntityState.Added]);
            await AssertStoredOptionsAsync(options, variantId, [("Uzunluk", "50 CM", 0)]);

            var compositeStates = await UpdateVariantAsync(
                options,
                variantId,
                "Renk / Beden",
                "Kırmızı / M");
            compositeStates.Should().BeEquivalentTo(
                [EntityState.Deleted, EntityState.Added, EntityState.Added]);
            await AssertStoredOptionsAsync(
                options,
                variantId,
                [("Renk", "Kırmızı", 0), ("Beden", "M", 1)]);

            var compositeReplacementStates = await UpdateVariantAsync(
                options,
                variantId,
                "Renk / Beden",
                "Kahverengi / L");
            compositeReplacementStates.Should().BeEquivalentTo(
                [EntityState.Deleted, EntityState.Deleted, EntityState.Added, EntityState.Added]);
            await AssertStoredOptionsAsync(
                options,
                variantId,
                [("Renk", "Kahverengi", 0), ("Beden", "L", 1)]);

            await AssertRealVariantConcurrencyStillFailsAsync(options, variantId);

            await using var metadataContext = new AppDbContext(options);
            metadataContext.Model
                .FindEntityType(typeof(ProductVariantOptionValue))!
                .FindProperty(nameof(ProductVariantOptionValue.Id))!
                .ValueGenerated.Should().Be(ValueGenerated.Never);
        }
        finally
        {
            await using var cleanupContext = new AppDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    // Burada izole SQL Server veritabanına varsayılan seçenek bağlantısı olmayan bir ürün varyantı kaydediyorum.
    private static async Task<Guid> SeedDefaultVariantAsync(DbContextOptions<AppDbContext> options)
    {
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var product = new Product("Kolye", "kolye", "KOLYE-MAIN");
        var variant = new ProductVariant(
            product,
            "Varsayılan",
            "KOLYE-DEFAULT",
            100m,
            0,
            value: "Standart");
        product.Variants.Add(variant);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return variant.Id;
    }

    // Burada gerçek handler çalışırken seçenek child kayıtlarının SaveChanges öncesindeki EF durumlarını yakalıyorum.
    private static async Task<IReadOnlyList<EntityState>> UpdateVariantAsync(
        DbContextOptions<AppDbContext> options,
        Guid variantId,
        string name,
        string value)
    {
        await using var context = new AppDbContext(options);
        var inspectingUnitOfWork = new InspectingUnitOfWork(context);
        var handler = new UpdateProductVariantCommandHandler(
            new ProductVariantRepository(context),
            inspectingUnitOfWork,
            new VariantOptionResolver(context));

        await handler.Handle(
            new UpdateProductVariantCommand(
                variantId,
                name,
                value,
                "KOLYE-DEFAULT",
                100m,
                0),
            CancellationToken.None);

        return inspectingUnitOfWork.ObservedOptionLinkStates;
    }

    // Burada kalıcı seçenek bağlantılarının ad, değer ve sırasını beklenen listeyle karşılaştırıyorum.
    private static async Task AssertStoredOptionsAsync(
        DbContextOptions<AppDbContext> options,
        Guid variantId,
        IReadOnlyList<(string Name, string Value, int DisplayOrder)> expected)
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

        stored.Select(item => (item.Name, item.Value, item.DisplayOrder))
            .Should().Equal(expected);
    }

    // Burada ProductVariant concurrency tokenının iki ayrı context arasındaki gerçek stale güncellemeyi hâlâ reddettiğini doğruluyorum.
    private static async Task AssertRealVariantConcurrencyStillFailsAsync(
        DbContextOptions<AppDbContext> options,
        Guid variantId)
    {
        await using var firstContext = new AppDbContext(options);
        await using var staleContext = new AppDbContext(options);
        var firstVariant = await firstContext.ProductVariants.SingleAsync(item => item.Id == variantId);
        var staleVariant = await staleContext.ProductVariants.SingleAsync(item => item.Id == variantId);

        firstVariant.UpdateDetails("Renk / Beden", "Kahverengi / XL", "KOLYE-DEFAULT", null, null);
        await new UnitOfWork(firstContext).SaveChangesAsync();

        staleVariant.UpdateDetails("Renk / Beden", "Kahverengi / XXL", "KOLYE-DEFAULT", null, null);
        var staleSave = () => new UnitOfWork(staleContext).SaveChangesAsync();

        await staleSave.Should().ThrowAsync<ConcurrencyException>();
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

    private sealed class InspectingUnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly UnitOfWork _inner;

        public IReadOnlyList<EntityState> ObservedOptionLinkStates { get; private set; } = [];

        // Burada gerçek UnitOfWork öncesinde ChangeTracker durumlarını gözlemlemek için sarmalayıcıyı hazırlıyorum.
        public InspectingUnitOfWork(AppDbContext context)
        {
            _context = context;
            _inner = new UnitOfWork(context);
        }

        // Burada seçenek bağlantılarının durumlarını kaydedip gerçek persistence davranışını değiştirmeden SaveChanges çağırıyorum.
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ObservedOptionLinkStates = _context.ChangeTracker
                .Entries<ProductVariantOptionValue>()
                .Select(entry => entry.State)
                .Where(state => state != EntityState.Unchanged)
                .ToList();
            return _inner.SaveChangesAsync(cancellationToken);
        }

        // Burada test sarmalayıcısının transaction davranışını gerçek UnitOfWork'e aynen aktarıyorum.
        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            return _inner.ExecuteInSerializableTransactionAsync(operation, cancellationToken);
        }
    }
}

internal sealed class SqlServerFactAttribute : FactAttribute
{
    // Burada SQL Server bağlantısı bulunmayan platformlarda provider testini açık gerekçeyle atlıyorum.
    public SqlServerFactAttribute()
    {
        var hasConfiguredSqlServer =
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ECOMMERCE_TEST_SQL_SERVER")) &&
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DB_PASSWORD"));
        if (!hasConfiguredSqlServer && !OperatingSystem.IsWindows())
        {
            Skip = "SQL Server integration test requires ECOMMERCE_TEST_SQL_SERVER and DB_PASSWORD.";
        }
    }
}
