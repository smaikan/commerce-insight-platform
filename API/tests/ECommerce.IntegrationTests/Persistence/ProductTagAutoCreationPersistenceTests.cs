using ECommerce.Application.Common.Services;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class ProductTagAutoCreationPersistenceTests
{
    // Burada aynı yeni etiketin iki üründe tek Tag kaydı ve iki ilişki olarak atomik kaydedildiğini doğruluyorum.
    [Fact]
    public async Task Database_Should_Create_One_Tag_And_Link_It_To_Multiple_Products()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var resolver = new ProductTagResolver(
            new TagRepository(context),
            new ProductUrlGenerator());
        var resolution = await resolver.ResolveAsync(["Shared Tag", " shared tag "]);
        var tagId = resolution.GetIds(["Shared Tag"]).Single();
        var firstProduct = new Product("First", "first", "FIRST-MAIN");
        var secondProduct = new Product("Second", "second", "SECOND-MAIN");
        firstProduct.ProductTags.Add(new ProductTag(firstProduct, tagId));
        secondProduct.ProductTags.Add(new ProductTag(secondProduct, tagId));

        context.Products.AddRange(firstProduct, secondProduct);
        await context.SaveChangesAsync();
        var loadedProduct = await new ProductRepository(context)
            .GetByIdAsync(firstProduct.Id);

        (await context.Tags.CountAsync()).Should().Be(1);
        (await context.ProductTags.CountAsync()).Should().Be(2);
        (await context.Tags.SingleAsync()).Name.Should().Be("Shared Tag");
        loadedProduct.Should().NotBeNull();
        loadedProduct!.ToDto().Tags.Should().ContainSingle(tag =>
            tag.Id == tagId && tag.Name == "Shared Tag");
    }

    // Burada farklı büyük-küçük harfle girilen mevcut etiket için yeni Tag satırı açılmadığını doğruluyorum.
    [Fact]
    public async Task Database_Should_Reuse_Existing_Tag_Case_Insensitively()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var existingTag = new Tag("Summer", "summer-custom");
        context.Tags.Add(existingTag);
        await context.SaveChangesAsync();
        var resolver = new ProductTagResolver(
            new TagRepository(context),
            new ProductUrlGenerator());

        var resolution = await resolver.ResolveAsync([" summer "]);
        await context.SaveChangesAsync();

        resolution.GetIds(["SUMMER"]).Should().ContainSingle()
            .Which.Should().Be(existingTag.Id);
        (await context.Tags.CountAsync()).Should().Be(1);
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
