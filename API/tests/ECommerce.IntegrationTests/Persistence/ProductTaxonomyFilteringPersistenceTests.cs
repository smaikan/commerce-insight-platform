using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class ProductTaxonomyFilteringPersistenceTests
{
    // Burada admin listesinin tür, marka, koleksiyon ve etiket filtrelerini AND mantığıyla uyguladığını doğruluyorum.
    [Fact]
    public async Task Admin_Reader_Should_Filter_By_All_Taxonomies_Without_Mixing_Products()
    {
        await using var fixture = await CreateFixtureAsync();

        var result = await new ProductListReader(fixture.Context).GetListAsync(new ProductListFilter(
            1,
            20,
            TypeId: fixture.TypeId,
            BrandId: fixture.BrandId,
            CollectionId: fixture.CollectionId,
            TagId: fixture.TagId,
            Status: ProductStatus.Active));

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(product => product.Title == "Matching Product");
    }

    // Burada storefront listesinin dört sınıflandırma filtresini uygulayıp taslak ürünü dışladığını doğruluyorum.
    [Fact]
    public async Task Storefront_Reader_Should_Filter_By_All_Taxonomies_And_Return_Only_Published_Product()
    {
        await using var fixture = await CreateFixtureAsync();

        var result = await new PublishedProductListReader(fixture.Context).GetListAsync(
            new PublishedProductListFilter(
                1,
                24,
                fixture.TypeId,
                fixture.BrandId,
                fixture.CollectionId,
                fixture.TagId));

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(product => product.Title == "Matching Product");
    }

    // Burada her sınıflandırma boyutunda farklı kayıtlar içeren ilişkisel test verisini hazırlıyorum.
    private static async Task<ProductFilterFixture> CreateFixtureAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var targetType = new ProductType("Target Type");
        var otherType = new ProductType("Other Type");
        var targetBrand = new Brand("Target Brand", "target-brand");
        var otherBrand = new Brand("Other Brand", "other-brand");
        var targetCollection = new Collection("Target Collection", "target-collection");
        var otherCollection = new Collection("Other Collection", "other-collection");
        var targetTag = new Tag("Target Tag", "target-tag");
        var otherTag = new Tag("Other Tag", "other-tag");

        var matching = CreateProduct("Matching Product", "matching-product", "MATCHING", targetType.Id,
            targetBrand.Id, targetCollection.Id, targetTag.Id, ProductStatus.Active);
        var wrongType = CreateProduct("Wrong Type", "wrong-type", "WRONG-TYPE", otherType.Id,
            targetBrand.Id, targetCollection.Id, targetTag.Id, ProductStatus.Active);
        var wrongBrand = CreateProduct("Wrong Brand", "wrong-brand", "WRONG-BRAND", targetType.Id,
            otherBrand.Id, targetCollection.Id, targetTag.Id, ProductStatus.Active);
        var wrongCollection = CreateProduct("Wrong Collection", "wrong-collection", "WRONG-COLLECTION", targetType.Id,
            targetBrand.Id, otherCollection.Id, targetTag.Id, ProductStatus.Active);
        var wrongTag = CreateProduct("Wrong Tag", "wrong-tag", "WRONG-TAG", targetType.Id,
            targetBrand.Id, targetCollection.Id, otherTag.Id, ProductStatus.Active);
        var draft = CreateProduct("Draft Match", "draft-match", "DRAFT-MATCH", targetType.Id,
            targetBrand.Id, targetCollection.Id, targetTag.Id, ProductStatus.Draft);

        context.AddRange(
            targetType, otherType, targetBrand, otherBrand, targetCollection, otherCollection, targetTag, otherTag,
            matching, wrongType, wrongBrand, wrongCollection, wrongTag, draft);
        await context.SaveChangesAsync();

        return new ProductFilterFixture(
            connection,
            context,
            targetType.Id,
            targetBrand.Id,
            targetCollection.Id,
            targetTag.Id);
    }

    // Burada test ürününü tek varyant ve seçili sınıflandırma ilişkileriyle oluşturuyorum.
    private static Product CreateProduct(
        string title,
        string url,
        string sku,
        Guid typeId,
        Guid brandId,
        Guid collectionId,
        Guid tagId,
        ProductStatus status)
    {
        var product = new Product(title, url, $"{sku}-MAIN", typeId, brandId, status: status);
        product.Variants.Add(new ProductVariant(product, "Standard", $"{sku}-STD", 100m, 5));
        product.ProductCollections.Add(new ProductCollection(product, collectionId));
        product.ProductTags.Add(new ProductTag(product, tagId));
        return product;
    }

    // Burada SQLite bağlantısı ve DbContext yaşam döngüsünü birlikte kapatıyorum.
    private sealed class ProductFilterFixture(
        SqliteConnection connection,
        AppDbContext context,
        Guid typeId,
        Guid brandId,
        Guid collectionId,
        Guid tagId) : IAsyncDisposable
    {
        public AppDbContext Context { get; } = context;
        public Guid TypeId { get; } = typeId;
        public Guid BrandId { get; } = brandId;
        public Guid CollectionId { get; } = collectionId;
        public Guid TagId { get; } = tagId;

        // Burada test veritabanı bağlamını ve bağlantısını güvenli sırayla kapatıyorum.
        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
