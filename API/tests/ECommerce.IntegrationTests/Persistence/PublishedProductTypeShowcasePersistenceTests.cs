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

public sealed class PublishedProductTypeShowcasePersistenceTests
{
    // Burada kategori vitrininin özel görsel, popüler ürün fallback'i, null ve yayın filtrelerini toplu doğruluyorum.
    [Fact]
    public async Task GetList_Should_Project_Effective_Images_And_Exclude_NonPublic_Types_Without_N_Plus_One()
    {
        await using var fixture = await ShowcaseFixture.CreateAsync();
        var reader = new PublishedProductTypeShowcaseReader(fixture.Context);

        fixture.CommandCounter.Reset();
        var result = await reader.GetListAsync(new PublishedProductTypeShowcaseFilter(1, 20, true, true));

        fixture.CommandCounter.ReaderCommandCount.Should().Be(2);
        result.TotalCount.Should().Be(3);
        result.Items.Select(item => item.Id).Should().Equal(
            fixture.FallbackTypeId,
            fixture.NoImageTypeId,
            fixture.OwnImageTypeId);
        result.Items[0].ProductCount.Should().Be(2);
        result.Items[0].ImageUrl.Should().Be("https://cdn.example.test/popular-main.webp");
        result.Items[1].ProductCount.Should().Be(1);
        result.Items[1].ImageUrl.Should().BeNull();
        result.Items[2].ImageUrl.Should().Be("https://cdn.example.test/type.webp");
    }

    // Burada kategori vitrini sayfalamasının filtrelenmiş toplamı koruduğunu doğruluyorum.
    [Fact]
    public async Task GetList_Should_Page_After_Public_Filtering_And_Preserve_Total_Count()
    {
        await using var fixture = await ShowcaseFixture.CreateAsync();
        var reader = new PublishedProductTypeShowcaseReader(fixture.Context);

        var result = await reader.GetListAsync(new PublishedProductTypeShowcaseFilter(2, 1, true, true));

        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(1);
        result.TotalCount.Should().Be(3);
        result.TotalPages.Should().Be(3);
        result.Items.Should().ContainSingle(item => item.Id == fixture.NoImageTypeId);
    }

    private sealed class ShowcaseFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public AppDbContext Context { get; }
        public CommandCounterInterceptor CommandCounter { get; }
        public Guid OwnImageTypeId { get; }
        public Guid FallbackTypeId { get; }
        public Guid NoImageTypeId { get; }

        // Burada kategori vitrini fixture bağımlılıklarını ve beklenen kimlikleri birlikte saklıyorum.
        private ShowcaseFixture(
            SqliteConnection connection,
            AppDbContext context,
            CommandCounterInterceptor commandCounter,
            ProductType ownImageType,
            ProductType fallbackType,
            ProductType noImageType)
        {
            _connection = connection;
            Context = context;
            CommandCounter = commandCounter;
            OwnImageTypeId = ownImageType.Id;
            FallbackTypeId = fallbackType.Id;
            NoImageTypeId = noImageType.Id;
        }

        // Burada özel görsel, popüler fallback ve dışlanacak kategorileri içeren ilişkisel fixture'ı kuruyorum.
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

            var ownImageType = new ProductType(
                "Own image", imageUrl: "https://cdn.example.test/type.webp");
            var fallbackType = new ProductType("Fallback");
            var noImageType = new ProductType("No image");
            var inactiveType = new ProductType("Inactive", isActive: false);
            var emptyType = new ProductType("Empty");
            var draftOnlyType = new ProductType("Draft only");

            var ownProduct = CreateProduct(
                "Own product", "own-product", "OWN", ownImageType, ProductStatus.Active,
                "https://cdn.example.test/own-product.webp");
            var lessPopularProduct = CreateProduct(
                "Less popular", "less-popular", "LESS", fallbackType, ProductStatus.Active,
                "https://cdn.example.test/less-popular.webp");
            var popularProduct = CreateProduct(
                "Popular", "popular", "POPULAR", fallbackType, ProductStatus.Active,
                "https://cdn.example.test/popular-secondary.webp",
                "https://cdn.example.test/popular-main.webp");
            popularProduct.IncreaseTotalPurchaseCount(1);
            var noImageProduct = CreateProduct(
                "No image product", "no-image-product", "NO-IMAGE", noImageType, ProductStatus.Active);
            var inactiveTypeProduct = CreateProduct(
                "Inactive type product", "inactive-type-product", "INACTIVE-TYPE",
                inactiveType, ProductStatus.Active, "https://cdn.example.test/inactive.webp");
            var draftProduct = CreateProduct(
                "Draft product", "draft-product", "DRAFT", draftOnlyType, ProductStatus.Draft,
                "https://cdn.example.test/draft.webp");
            var inactivePopularProduct = CreateProduct(
                "Inactive popular", "inactive-popular", "INACTIVE-POPULAR", fallbackType,
                ProductStatus.Active, "https://cdn.example.test/inactive-popular.webp", isActive: false);
            inactivePopularProduct.IncreaseTotalPurchaseCount(5);

            context.AddRange(
                ownImageType,
                fallbackType,
                noImageType,
                inactiveType,
                emptyType,
                draftOnlyType,
                ownProduct,
                lessPopularProduct,
                popularProduct,
                noImageProduct,
                inactiveTypeProduct,
                draftProduct,
                inactivePopularProduct);
            await context.SaveChangesAsync();
            commandCounter.Reset();

            return new ShowcaseFixture(
                connection,
                context,
                commandCounter,
                ownImageType,
                fallbackType,
                noImageType);
        }

        // Burada test ürününü kategori ilişkisi ve kararlı sıralanacak görselleriyle oluşturuyorum.
        private static Product CreateProduct(
            string title,
            string url,
            string sku,
            ProductType productType,
            ProductStatus status,
            string? secondaryImageUrl = null,
            string? mainImageUrl = null,
            bool isActive = true)
        {
            var product = new Product(
                title,
                url,
                $"{sku}-MAIN",
                typeId: productType.Id,
                status: status,
                isActive: isActive);
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

        // Burada doğrulanacak vitrin sorgularından önce komut sayacını sıfırlıyorum.
        public void Reset()
        {
            ReaderCommandCount = 0;
        }

        // Burada reader komutlarını sayarak sorgu sayısının kategori adediyle büyümediğini ölçüyorum.
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
