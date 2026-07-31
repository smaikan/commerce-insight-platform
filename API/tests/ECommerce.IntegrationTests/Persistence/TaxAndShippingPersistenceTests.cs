using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class TaxAndShippingPersistenceTests
{
    // Burada ürüne bağlı vergi oranının kalıcı ilişki olarak kaydedilip okunduğunu doğruluyorum.
    [Fact]
    public async Task Database_Should_Persist_Product_TaxRate_Relationship()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var taxRate = new TaxRate("KDV", 20m);
        var product = new Product(
            "Vergili Ürün",
            "vergili-urun",
            "TAXED-PRODUCT-MAIN",
            taxRateId: taxRate.Id);
        context.AddRange(taxRate, product);

        await context.SaveChangesAsync();

        var savedProduct = await context.Products
            .Include(item => item.TaxRate)
            .SingleAsync(item => item.Id == product.Id);
        savedProduct.TaxRateId.Should().Be(taxRate.Id);
        savedProduct.TaxRate.Should().NotBeNull();
        savedProduct.TaxRate!.Rate.Should().Be(20m);
    }

    // Burada checkout için kullanılan takipli toplu ürün sorgusunun vergi oranı navigation'ını da yüklediğini doğruluyorum.
    [Fact]
    public async Task Checkout_Product_Query_Should_Load_The_Assigned_TaxRate()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        var taxRate = new TaxRate("KDV", 20m);
        var product = new Product(
            "Vergili Ürün",
            "vergili-urun-checkout",
            "TAXED-CHECKOUT-MAIN",
            taxRateId: taxRate.Id);

        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.AddRange(taxRate, product);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = new AppDbContext(options);
        var repository = new ProductRepository(readContext);

        var result = await repository.GetByIdsForUpdateAsync([product.Id]);

        result.Should().ContainSingle();
        result.Single().TaxRate.Should().NotBeNull();
        result.Single().TaxRate!.Rate.Should().Be(20m);
    }

    // Burada vergi oranı ve kargo yöntemi adlarının veritabanında benzersiz tanımlandığını doğruluyorum.
    [Fact]
    public async Task Database_Should_Reject_Duplicate_TaxRate_And_ShippingMethod_Names()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Set<TaxRate>().AddRange(new TaxRate("KDV", 20m), new TaxRate("KDV", 10m));

        var taxRateAct = () => context.SaveChangesAsync();

        await taxRateAct.Should().ThrowAsync<DbUpdateException>();

        context.ChangeTracker.Clear();
        context.Set<ShippingMethod>().AddRange(
            new ShippingMethod("Standart", 49.90m),
            new ShippingMethod("Standart", 99.90m));

        var shippingMethodAct = () => context.SaveChangesAsync();

        await shippingMethodAct.Should().ThrowAsync<DbUpdateException>();
    }

    // Burada ürünün vergi oranı bağlantısının migration sırasında eski ürünleri koruyacak şekilde nullable olduğunu doğruluyorum.
    [Fact]
    public async Task Model_Should_Configure_Product_TaxRate_As_Optional()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        var productEntity = context.Model.FindEntityType(typeof(Product));
        var taxRateProperty = productEntity!.FindProperty(nameof(Product.TaxRateId));
        var foreignKey = productEntity.GetForeignKeys()
            .Single(item => item.Properties.Single().Name == nameof(Product.TaxRateId));

        taxRateProperty.Should().NotBeNull();
        taxRateProperty!.IsNullable.Should().BeTrue();
        foreignKey.DeleteBehavior.Should().Be(DeleteBehavior.SetNull);
    }

    // Burada kargo yöntemlerinin gösterim sırasına göre istenen aktif kayıtlara uygun indekse sahip olduğunu doğruluyorum.
    [Fact]
    public async Task Model_Should_Configure_ShippingMethod_Active_DisplayOrder_Index()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        var shippingMethodEntity = context.Model.FindEntityType(typeof(ShippingMethod));
        var index = shippingMethodEntity!.GetIndexes().Single(item =>
            item.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(ShippingMethod.IsActive), nameof(ShippingMethod.DisplayOrder)]));

        index.Should().NotBeNull();
    }

    // Burada ilişkisel testler için açık SQLite bağlantısı oluşturuyorum.
    private static async Task<SqliteConnection> CreateOpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    // Burada test DbContext ayarlarını açık SQLite bağlantısına bağlıyorum.
    private static DbContextOptions<AppDbContext> CreateOptions(SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
    }
}
