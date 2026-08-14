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

public sealed class PublishedCollectionShowcasePersistenceTests
{
    // Burada koleksiyon vitrininin özel görsel, ürün fallback'i, null ve yayın filtrelerini sabit sorgu sayısıyla doğruluyorum.
    [Fact]
    public async Task GetList_Should_Project_Effective_Images_And_Exclude_NonPublic_Collections_Without_N_Plus_One()
    {
        await using var fixture = await ShowcaseFixture.CreateAsync();
        var reader = new PublishedCollectionShowcaseReader(fixture.Context);

        fixture.CommandCounter.Reset();
        var result = await reader.GetListAsync(new PublishedCollectionShowcaseFilter(1, 20, true, true));

        fixture.CommandCounter.ReaderCommandCount.Should().Be(2);
        result.TotalCount.Should().Be(3);
        result.Items.Select(item => item.Id).Should().Equal(
            fixture.OwnImageCollectionId,
            fixture.FallbackCollectionId,
            fixture.NoImageCollectionId);
        result.Items[0].Should().BeEquivalentTo(new
        {
            Id = fixture.OwnImageCollectionId,
            Name = "Own image",
            Url = "own-image",
            ProductCount = 1,
            IsFeatured = true,
            DisplayOrder = 0,
            ImageUrl = "https://cdn.example.test/collection.webp"
        });
        result.Items[1].ProductCount.Should().Be(2);
        result.Items[1].ImageUrl.Should().Be("https://cdn.example.test/first-main.webp");
        result.Items[2].ProductCount.Should().Be(1);
        result.Items[2].ImageUrl.Should().BeNull();
    }

    // Burada sayfalamanın filtrelenmiş toplamı koruyup koleksiyon kartı sırasını değiştirmediğini doğruluyorum.
    [Fact]
    public async Task GetList_Should_Page_After_Public_Filtering_And_Preserve_Total_Count()
    {
        await using var fixture = await ShowcaseFixture.CreateAsync();
        var reader = new PublishedCollectionShowcaseReader(fixture.Context);

        var result = await reader.GetListAsync(new PublishedCollectionShowcaseFilter(2, 1, true, true));

        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(1);
        result.TotalCount.Should().Be(3);
        result.TotalPages.Should().Be(3);
        result.Items.Should().ContainSingle(item => item.Id == fixture.FallbackCollectionId);
    }

    private sealed class ShowcaseFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public AppDbContext Context { get; }
        public CommandCounterInterceptor CommandCounter { get; }
        public Guid OwnImageCollectionId { get; }
        public Guid FallbackCollectionId { get; }
        public Guid NoImageCollectionId { get; }

        // Burada koleksiyon vitrin fixture bağımlılıklarını ve beklenen kimlikleri birlikte saklıyorum.
        private ShowcaseFixture(
            SqliteConnection connection,
            AppDbContext context,
            CommandCounterInterceptor commandCounter,
            Collection ownImageCollection,
            Collection fallbackCollection,
            Collection noImageCollection)
        {
            _connection = connection;
            Context = context;
            CommandCounter = commandCounter;
            OwnImageCollectionId = ownImageCollection.Id;
            FallbackCollectionId = fallbackCollection.Id;
            NoImageCollectionId = noImageCollection.Id;
        }

        // Burada özel görsel, ürün fallback'i ve dışlanacak koleksiyonları içeren ilişkisel fixture'ı kuruyorum.
        public static async Task<ShowcaseFixture> CreateAsync()
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

            var ownImageCollection = new Collection(
                "Own image", "own-image", isFeatured: true, displayOrder: 0,
                imageUrl: "https://cdn.example.test/collection.webp");
            var fallbackCollection = new Collection("Fallback", "fallback", displayOrder: 1);
            var noImageCollection = new Collection("No image", "no-image", displayOrder: 2);
            var inactiveCollection = new Collection("Inactive", "inactive", isActive: false, displayOrder: 3);
            var emptyCollection = new Collection("Empty", "empty", displayOrder: 4);
            var draftOnlyCollection = new Collection("Draft only", "draft-only", displayOrder: 5);

            var ownProduct = CreateProduct(
                "Own product", "own-product", "OWN", ownImageCollection, 0, ProductStatus.Active,
                "https://cdn.example.test/own-product.webp");
            var firstFallbackProduct = CreateProduct(
                "Alpha product", "alpha-product", "ALPHA", fallbackCollection, 0, ProductStatus.Active,
                "https://cdn.example.test/first-secondary.webp",
                "https://cdn.example.test/first-main.webp");
            var secondFallbackProduct = CreateProduct(
                "Beta product", "beta-product", "BETA", fallbackCollection, 1, ProductStatus.Active,
                "https://cdn.example.test/second-main.webp");
            var noImageProduct = CreateProduct(
                "No image product", "no-image-product", "NO-IMAGE", noImageCollection, 0, ProductStatus.Active);
            var inactiveCollectionProduct = CreateProduct(
                "Inactive collection product", "inactive-collection-product", "INACTIVE-COLLECTION",
                inactiveCollection, 0, ProductStatus.Active, "https://cdn.example.test/inactive.webp");
            var draftProduct = CreateProduct(
                "Draft product", "draft-product", "DRAFT", draftOnlyCollection, 0, ProductStatus.Draft,
                "https://cdn.example.test/draft.webp");
            var inactiveProduct = CreateProduct(
                "Inactive product", "inactive-product", "INACTIVE", fallbackCollection, 2,
                ProductStatus.Active, "https://cdn.example.test/inactive-product.webp", isActive: false);

            context.AddRange(
                ownImageCollection,
                fallbackCollection,
                noImageCollection,
                inactiveCollection,
                emptyCollection,
                draftOnlyCollection,
                ownProduct,
                firstFallbackProduct,
                secondFallbackProduct,
                noImageProduct,
                inactiveCollectionProduct,
                draftProduct,
                inactiveProduct);
            await context.SaveChangesAsync();
            commandCounter.Reset();

            return new ShowcaseFixture(
                connection,
                context,
                commandCounter,
                ownImageCollection,
                fallbackCollection,
                noImageCollection);
        }

        // Burada test ürününü koleksiyon ilişkisi ve kararlı sıralanacak görselleriyle oluşturuyorum.
        private static Product CreateProduct(
            string title,
            string url,
            string sku,
            Collection collection,
            int displayOrder,
            ProductStatus status,
            string? secondaryImageUrl = null,
            string? mainImageUrl = null,
            bool isActive = true)
        {
            var product = new Product(
                title,
                url,
                $"{sku}-MAIN",
                status: status,
                isActive: isActive,
                displayOrder: displayOrder);
            product.ProductCollections.Add(new ProductCollection(product, collection.Id));
            if (secondaryImageUrl is not null)
            {
                product.Images.Add(new ProductImage(product, secondaryImageUrl, displayOrder: 0));
            }

            if (mainImageUrl is not null)
            {
                product.Images.Add(new ProductImage(product, mainImageUrl, displayOrder: 5, isMain: true));
            }

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

        // Burada yalnız doğrulanacak vitrin sorgularından önce komut sayacını sıfırlıyorum.
        public void Reset()
        {
            ReaderCommandCount = 0;
        }

        // Burada ilişkisel reader komutlarını sayarak sorgu sayısının koleksiyon adediyle büyümediğini ölçüyorum.
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
