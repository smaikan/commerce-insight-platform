using ECommerce.Domain.Entities;
using ECommerce.Application.Products.Variants.Commands.UpdateProductVariant;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using ECommerce.Persistence.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class VariantOptionPersistenceTests
{
    // Burada Ebat adına bağlı iki değerin ayrı satırlarda aynı foreign key ile kaydedildiğini doğruluyorum.
    [Fact]
    public async Task Resolver_Should_Create_Two_Values_Under_One_Option_Name()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var resolver = new VariantOptionResolver(context);

        var first = await resolver.ResolveAsync("Ebat", "100x150");
        var second = await resolver.ResolveAsync("Ebat", "200x250");
        await context.SaveChangesAsync();

        (await context.VariantOptionNames.CountAsync()).Should().Be(1);
        (await context.VariantOptionValues.CountAsync()).Should().Be(2);
        first.Name.Id.Should().Be(second.Name.Id);
        first.Value.VariantOptionNameId.Should().Be(first.Name.Id);
        second.Value.VariantOptionNameId.Should().Be(second.Name.Id);
    }

    // Burada büyük-küçük harfi farklı varyant adlarının ayrı merkezi kayıtlar oluşturduğunu doğruluyorum.
    [Fact]
    public async Task Resolver_Should_Treat_Option_Names_Case_Sensitively()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var resolver = new VariantOptionResolver(context);

        var upper = await resolver.ResolveAsync("Ebat", "100x150");
        var lower = await resolver.ResolveAsync("ebat", "100x150");
        await context.SaveChangesAsync();

        (await context.VariantOptionNames.CountAsync()).Should().Be(2);
        upper.Name.Id.Should().NotBe(lower.Name.Id);
    }

    // Burada ProductVariant metin alanları ile bağlı merkezi ad-değer kayıtlarının aynı kaldığını doğruluyorum.
    [Fact]
    public async Task ProductVariant_Should_Persist_The_Resolved_Option_Relationships()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var resolver = new VariantOptionResolver(context);
        var option = await resolver.ResolveAsync("Ebat", "100x150");
        var product = new Product("Halı", "hali", "HALI-001");
        var variant = new ProductVariant(product, "Ebat", "HALI-001-100X150", 100m, 5, value: "100x150");
        variant.AssignVariantOption(option.Name, option.Value);
        product.Variants.Add(variant);

        context.Products.Add(product);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var saved = await context.ProductVariants.SingleAsync();

        saved.Name.Should().Be("Ebat");
        saved.Value.Should().Be("100x150");
        saved.VariantOptionNameId.Should().Be(option.Name.Id);
        saved.VariantOptionValueId.Should().Be(option.Value.Id);
    }

    // Burada mevcut tekli ve çoklu seçeneklerin düzenlemede tekrar bağ eklemediğini doğruluyorum.
    [Theory]
    [InlineData("Renk", "Gümüş", 1)]
    [InlineData("Renk / Boyut", "Gümüş / Mini", 2)]
    public async Task UpdateProductVariant_Should_Keep_Existing_Option_Relationships(
        string optionName,
        string optionValue,
        int expectedOptionCount)
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        Guid variantId;

        await using (var seedContext = new AppDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            var resolver = new VariantOptionResolver(seedContext);
            var selections = await resolver.ResolveCompositeAsync(optionName, optionValue);
            var product = new Product("Kolye", "kolye", "KOLYE-001");
            var variant = new ProductVariant(product, optionName, "KOLYE-001-GUMUS-MINI", 100m, 5, value: optionValue);
            variant.ReplaceOptionValues(selections);
            product.Variants.Add(variant);
            seedContext.Products.Add(product);
            await seedContext.SaveChangesAsync();
            variantId = variant.Id;
        }

        await using (var updateContext = new AppDbContext(options))
        {
            var handler = new UpdateProductVariantCommandHandler(
                new ProductVariantRepository(updateContext),
                new UnitOfWork(updateContext),
                new VariantOptionResolver(updateContext));

            await handler.Handle(new UpdateProductVariantCommand(
                variantId,
                optionName,
                optionValue,
                "KOLYE-001-GUMUS-MINI",
                120m,
                5), CancellationToken.None);
        }

        await using var assertionContext = new AppDbContext(options);
        (await assertionContext.ProductVariantOptionValues
            .Where(item => item.ProductVariantId == variantId)
            .CountAsync()).Should().Be(expectedOptionCount);
    }

    // Burada büyük-küçük harf duyarlı SQLite karşılaştırmasını SQL Server test kolasyonuna yakın şekilde kaydediyorum.
    private static async Task<SqliteConnection> CreateOpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.CreateCollation("Turkish_100_CS_AS", StringComparer.Ordinal.Compare);
        await connection.OpenAsync();
        return connection;
    }

    // Burada ilişkisel kalıcılık testinin DbContext seçeneklerini açık SQLite bağlantısına bağlıyorum.
    private static DbContextOptions<AppDbContext> CreateOptions(SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
    }
}
