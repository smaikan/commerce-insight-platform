using System.Data.Common;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class PublishedProductFacetPersistenceTests
{
    // Burada üç facet boyutunun aktif ve yayımlanmış ürünleri tek SQL sorgusuyla doğru saydığını doğruluyorum.
    [Fact]
    public async Task Facets_Should_Return_Only_Active_Options_With_Published_Product_Counts_In_One_Query()
    {
        await using var fixture = await FacetFixture.CreateAsync();
        var reader = new PublishedProductFacetReader(fixture.Context);

        fixture.CommandCounter.Reset();
        var brands = await reader.GetFacetsAsync(PublishedProductFacetDimension.Brand, new());
        fixture.CommandCounter.ReaderCommandCount.Should().Be(1);

        fixture.CommandCounter.Reset();
        var collections = await reader.GetFacetsAsync(PublishedProductFacetDimension.Collection, new());
        fixture.CommandCounter.ReaderCommandCount.Should().Be(1);

        fixture.CommandCounter.Reset();
        var productTypes = await reader.GetFacetsAsync(PublishedProductFacetDimension.ProductType, new());
        fixture.CommandCounter.ReaderCommandCount.Should().Be(1);

        brands.Should().BeEquivalentTo(
            [
                new { Id = fixture.AlphaBrandId, Name = "Alpha", ProductCount = 2 },
                new { Id = fixture.BetaBrandId, Name = "Beta", ProductCount = 1 }
            ]);
        collections.Should().BeEquivalentTo(
            [
                new { Id = fixture.SummerCollectionId, Name = "Summer", ProductCount = 2 },
                new { Id = fixture.WinterCollectionId, Name = "Winter", ProductCount = 1 }
            ]);
        productTypes.Should().BeEquivalentTo(
            [
                new { Id = fixture.ShirtTypeId, Name = "Shirt", ProductCount = 2 },
                new { Id = fixture.ShoeTypeId, Name = "Shoe", ProductCount = 1 }
            ]);
    }

    // Burada her endpointin kendi boyut filtresini dışlayıp diğer seçili boyutları AND mantığıyla uyguladığını doğruluyorum.
    [Fact]
    public async Task Facets_Should_Ignore_Own_Dimension_And_Apply_Other_Selected_Dimensions()
    {
        await using var fixture = await FacetFixture.CreateAsync();
        var reader = new PublishedProductFacetReader(fixture.Context);

        var brands = await reader.GetFacetsAsync(
            PublishedProductFacetDimension.Brand,
            new PublishedProductFacetFilter(
                fixture.ShirtTypeId,
                fixture.BetaBrandId,
                fixture.SummerCollectionId,
                fixture.SaleTagId));
        var collections = await reader.GetFacetsAsync(
            PublishedProductFacetDimension.Collection,
            new PublishedProductFacetFilter(
                fixture.ShirtTypeId,
                fixture.AlphaBrandId,
                fixture.WinterCollectionId,
                fixture.SaleTagId));
        var productTypes = await reader.GetFacetsAsync(
            PublishedProductFacetDimension.ProductType,
            new PublishedProductFacetFilter(
                fixture.ShoeTypeId,
                fixture.AlphaBrandId,
                fixture.SummerCollectionId,
                fixture.SaleTagId));

        brands.Should().HaveCount(2);
        brands.Should().OnlyContain(facet => facet.ProductCount == 1);
        collections.Should().ContainSingle(facet =>
            facet.Id == fixture.SummerCollectionId && facet.ProductCount == 1);
        productTypes.Should().ContainSingle(facet =>
            facet.Id == fixture.ShirtTypeId && facet.ProductCount == 1);
    }

    private sealed class FacetFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public AppDbContext Context { get; }
        public CommandCounterInterceptor CommandCounter { get; }
        public Guid ShirtTypeId { get; }
        public Guid ShoeTypeId { get; }
        public Guid AlphaBrandId { get; }
        public Guid BetaBrandId { get; }
        public Guid SummerCollectionId { get; }
        public Guid WinterCollectionId { get; }
        public Guid SaleTagId { get; }

        // Burada facet test veritabanı ile beklenen sınıflandırma kimliklerini birlikte saklıyorum.
        private FacetFixture(
            SqliteConnection connection,
            AppDbContext context,
            CommandCounterInterceptor commandCounter,
            ProductType shirtType,
            ProductType shoeType,
            Brand alphaBrand,
            Brand betaBrand,
            Collection summerCollection,
            Collection winterCollection,
            Tag saleTag)
        {
            _connection = connection;
            Context = context;
            CommandCounter = commandCounter;
            ShirtTypeId = shirtType.Id;
            ShoeTypeId = shoeType.Id;
            AlphaBrandId = alphaBrand.Id;
            BetaBrandId = betaBrand.Id;
            SummerCollectionId = summerCollection.Id;
            WinterCollectionId = winterCollection.Id;
            SaleTagId = saleTag.Id;
        }

        // Burada yayımlanmış, taslak, pasif ve boş facet örneklerini içeren ilişkisel fixture'ı kuruyorum.
        public static async Task<FacetFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
            await connection.OpenAsync();
            var commandCounter = new CommandCounterInterceptor();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(commandCounter)
                .Options;
            var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var shirtType = new ProductType("Shirt");
            var shoeType = new ProductType("Shoe");
            var emptyType = new ProductType("Empty type");
            var inactiveType = new ProductType("Inactive type", isActive: false);
            var alphaBrand = new Brand("Alpha", "alpha");
            var betaBrand = new Brand("Beta", "beta");
            var emptyBrand = new Brand("Empty brand", "empty-brand");
            var inactiveBrand = new Brand("Inactive brand", "inactive-brand", isActive: false);
            var summerCollection = new Collection("Summer", "summer");
            var winterCollection = new Collection("Winter", "winter");
            var emptyCollection = new Collection("Empty collection", "empty-collection");
            var inactiveCollection = new Collection("Inactive collection", "inactive-collection", isActive: false);
            var saleTag = new Tag("Sale", "sale");

            var alphaShirt = CreateProduct(
                "Alpha shirt", "alpha-shirt", "ALPHA-SHIRT", shirtType, alphaBrand,
                summerCollection, saleTag, ProductStatus.Active);
            var betaShirt = CreateProduct(
                "Beta shirt", "beta-shirt", "BETA-SHIRT", shirtType, betaBrand,
                summerCollection, saleTag, ProductStatus.Active);
            var alphaShoe = CreateProduct(
                "Alpha shoe", "alpha-shoe", "ALPHA-SHOE", shoeType, alphaBrand,
                winterCollection, saleTag, ProductStatus.Active);
            var draft = CreateProduct(
                "Draft", "draft", "DRAFT", shirtType, alphaBrand,
                summerCollection, saleTag, ProductStatus.Draft);
            var inactiveProduct = CreateProduct(
                "Inactive", "inactive", "INACTIVE", shirtType, alphaBrand,
                summerCollection, saleTag, ProductStatus.Active,
                isActive: false);
            var inactiveTaxonomyProduct = CreateProduct(
                "Inactive taxonomy", "inactive-taxonomy", "INACTIVE-TAXONOMY", inactiveType, inactiveBrand,
                inactiveCollection, saleTag, ProductStatus.Active);

            context.AddRange(
                shirtType, shoeType, emptyType, inactiveType,
                alphaBrand, betaBrand, emptyBrand, inactiveBrand,
                summerCollection, winterCollection, emptyCollection, inactiveCollection,
                saleTag,
                alphaShirt, betaShirt, alphaShoe, draft, inactiveProduct, inactiveTaxonomyProduct);
            await context.SaveChangesAsync();

            return new FacetFixture(
                connection,
                context,
                commandCounter,
                shirtType,
                shoeType,
                alphaBrand,
                betaBrand,
                summerCollection,
                winterCollection,
                saleTag);
        }

        // Burada tek ürünün facet ilişkilerini ve yayın durumunu test verisine ekliyorum.
        private static Product CreateProduct(
            string title,
            string url,
            string sku,
            ProductType type,
            Brand brand,
            Collection collection,
            Tag tag,
            ProductStatus status,
            bool isActive = true)
        {
            var product = new Product(
                title,
                url,
                $"{sku}-MAIN",
                type.Id,
                brand.Id,
                status: status,
                isActive: isActive);
            product.ProductCollections.Add(new ProductCollection(product, collection.Id));
            product.ProductTags.Add(new ProductTag(product, tag.Id));
            return product;
        }

        // Burada fixture DbContext ve SQLite bağlantısını güvenli sırayla kapatıyorum.
        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class CommandCounterInterceptor : DbCommandInterceptor
    {
        public int ReaderCommandCount { get; private set; }

        // Burada sadece doğrulanacak facet sorgularından önce sayaç değerini sıfırlıyorum.
        public void Reset()
        {
            ReaderCommandCount = 0;
        }

        // Burada ilişkisel reader komutlarını sayarak her facet çağrısının tek SQL çalıştırdığını ölçüyorum.
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ReaderCommandCount++;
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
