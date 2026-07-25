using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class ProductMainSkuPersistenceTests
{
    // Burada ana SKU değerinin kaydedilip okunabildiğini ve SKU ile ürün aranabildiğini doğruluyorum.
    [Fact]
    public async Task Database_Should_Persist_And_Search_Product_Main_Sku()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Products.Add(new Product(
            "Main SKU Product",
            "main-sku-product",
            mainSku: "CATALOG-MAIN-001"));
        await context.SaveChangesAsync();

        var result = await new ProductRepository(context)
            .GetListAsync(new ProductListFilter(1, 20, Search: "catalog-main-001"));

        result.Items.Should().ContainSingle();
        result.Items.Single().MainSku.Should().Be("CATALOG-MAIN-001");
    }

    // Burada büyük-küçük harf farkıyla yinelenen ana SKU değerinin veritabanına kaydedilmesini engelliyorum.
    [Fact]
    public async Task Database_Should_Reject_Duplicate_Product_Main_Sku()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Products.AddRange(
            new Product("First Product", "first-product", mainSku: "DUPLICATE-MAIN"),
            new Product("Second Product", "second-product", mainSku: "duplicate-main"));

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    // Burada MainSku alanının EF modelinde zorunlu, 100 karakter ve benzersiz olduğunu doğruluyorum.
    [Fact]
    public async Task Model_Should_Configure_Product_Main_Sku_Constraints()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        var productEntity = context.Model.FindEntityType(typeof(Product));
        var mainSkuProperty = productEntity!.FindProperty(nameof(Product.MainSku));
        var mainSkuIndex = productEntity.GetIndexes()
            .Single(index => index.Properties.Count == 1 &&
                index.Properties[0].Name == nameof(Product.MainSku));

        mainSkuProperty.Should().NotBeNull();
        mainSkuProperty!.IsNullable.Should().BeFalse();
        mainSkuProperty.GetMaxLength().Should().Be(Product.MaximumMainSkuLength);
        mainSkuIndex.IsUnique.Should().BeTrue();
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
