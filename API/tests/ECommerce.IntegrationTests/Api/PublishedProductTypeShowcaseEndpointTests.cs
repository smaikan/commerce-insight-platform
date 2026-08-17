using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Application.Common.Models;
using ECommerce.Application.ProductTypes.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ECommerce.IntegrationTests.Api;

public sealed class PublishedProductTypeShowcaseEndpointTests
{
    // Burada anonim kategori vitrininin özel görsel, popüler fallback ve eleme davranışını gerçek HTTP hattında doğruluyorum.
    [Fact]
    public async Task Published_Product_Types_Should_Return_Showcase_Cards_Through_Real_Http_Pipeline()
    {
        await using var scenario = await ShowcaseApiScenario.CreateAsync();

        using var response = await scenario.Client.GetAsync(
            "/api/product-types/published?PageNumber=1&PageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<
            PagedResult<PublishedProductTypeShowcaseItemDto>>();
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(3);
        result.Items.Select(item => item.Id).Should().Equal(
            scenario.FallbackTypeId,
            scenario.NoImageTypeId,
            scenario.OwnImageTypeId);
        result.Items[0].ImageUrl.Should().Be("https://cdn.example.test/popular-main.webp");
        result.Items[0].ProductCount.Should().Be(2);
        result.Items[1].ImageUrl.Should().BeNull();
        result.Items[2].ImageUrl.Should().Be("https://cdn.example.test/type.webp");
        scenario.CommandCounter.ReaderCommandCount.Should().Be(3);
    }

    // Burada geçersiz kategori vitrini sayfalamasının ortak 400 ProblemDetails ürettiğini doğruluyorum.
    [Fact]
    public async Task Published_Product_Types_Should_Return_Validation_Problem_For_Invalid_Page_Size()
    {
        await using var scenario = await ShowcaseApiScenario.CreateAsync();

        using var response = await scenario.Client.GetAsync(
            "/api/product-types/published?PageNumber=1&PageSize=101");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().Should().Be("bad_request");
        problem.RootElement.GetProperty("status").GetInt32().Should().Be(400);
    }

    // Burada OpenAPI'nin kategori görseli, anonim erişim ve sayfalama sözleşmesini yayınladığını doğruluyorum.
    [Fact]
    public async Task OpenApi_Should_Document_Published_Product_Type_Showcase_Contract()
    {
        await using var scenario = await ShowcaseApiScenario.CreateAsync();

        using var response = await scenario.Client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var operation = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/product-types/published")
            .GetProperty("get");
        operation.GetProperty("security").GetArrayLength().Should().Be(0);
        operation.GetProperty("parameters")
            .EnumerateArray()
            .Select(parameter => parameter.GetProperty("name").GetString())
            .Should().BeEquivalentTo(["PageNumber", "PageSize"]);

        var schema = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("PublishedProductTypeShowcaseItemDto");
        schema.GetProperty("required")
            .EnumerateArray()
            .Select(property => property.GetString())
            .Should().Contain(["id", "name", "productCount"]);
        var imageUrl = schema.GetProperty("properties").GetProperty("imageUrl");
        imageUrl.GetProperty("nullable").GetBoolean().Should().BeTrue();
        imageUrl.GetProperty("maxLength").GetInt32().Should().Be(500);

        var productTypeDto = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("ProductTypeDto");
        productTypeDto.GetProperty("properties").GetProperty("imageUrl")
            .GetProperty("nullable").GetBoolean().Should().BeTrue();
    }

    private sealed class ShowcaseApiScenario : IAsyncDisposable
    {
        private readonly ShowcaseApiFactory _factory;

        public HttpClient Client { get; }
        public Guid OwnImageTypeId { get; }
        public Guid FallbackTypeId { get; }
        public Guid NoImageTypeId { get; }
        public CommandCounterInterceptor CommandCounter { get; }

        // Burada HTTP client, host ve beklenen kategori kimliklerini aynı yaşam döngüsünde saklıyorum.
        private ShowcaseApiScenario(
            ShowcaseApiFactory factory,
            HttpClient client,
            ShowcaseSeed seed)
        {
            _factory = factory;
            Client = client;
            OwnImageTypeId = seed.OwnImageTypeId;
            FallbackTypeId = seed.FallbackTypeId;
            NoImageTypeId = seed.NoImageTypeId;
            CommandCounter = factory.CommandCounter;
        }

        // Burada izole API hostunu başlatıp public kategori vitrini verisini hazırlıyorum.
        public static async Task<ShowcaseApiScenario> CreateAsync()
        {
            var factory = new ShowcaseApiFactory();
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });
            var seed = await factory.InitializeAndSeedAsync();
            factory.CommandCounter.Reset();
            return new ShowcaseApiScenario(factory, client, seed);
        }

        // Burada test HTTP client'ı ile hostunu birlikte kapatıyorum.
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _factory.DisposeAsync();
        }
    }

    private sealed class ShowcaseApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:;Foreign Keys=True");

        public CommandCounterInterceptor CommandCounter { get; } = new();

        // Burada HTTP testleri boyunca ortak ilişkisel SQLite bağlantısını açık tutuyorum.
        public ShowcaseApiFactory()
        {
            _connection.Open();
        }

        // Burada gerçek API hattını SQLite ile çalıştırıp arka plan servislerini testten çıkarıyorum.
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "unused-by-test-override");
            builder.UseSetting("Jwt:Issuer", "ECommerce.ProductTypeShowcaseTests");
            builder.UseSetting("Jwt:Audience", "ECommerce.ProductTypeShowcaseTests.Client");
            builder.UseSetting("Jwt:SecretKey", "product-type-showcase-test-secret-key-at-least-32-bytes");
            builder.UseSetting(
                "DataProtection:KeyRingPath",
                Path.Combine(Path.GetTempPath(), "ECommerce.ProductTypeShowcaseTests.DataProtection"));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                foreach (var hostedService in services
                    .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
                    .ToList())
                {
                    services.Remove(hostedService);
                }

                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite(_connection).AddInterceptors(CommandCounter));
            });
        }

        // Burada endpointin özel görsel, popüler fallback, null ve public eleme sonuçlarını üretecek veriyi ekliyorum.
        public async Task<ShowcaseSeed> InitializeAndSeedAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync();

            var ownImageType = new ProductType(
                "Own image", imageUrl: "https://cdn.example.test/type.webp");
            var fallbackType = new ProductType("Fallback");
            var noImageType = new ProductType("No image");
            var inactiveType = new ProductType("Inactive", isActive: false);
            var emptyType = new ProductType("Empty");
            var draftOnlyType = new ProductType("Draft");

            var ownProduct = CreateProduct(
                "Own product", "own-product", "OWN", ownImageType, ProductStatus.Active,
                "https://cdn.example.test/own-product.webp");
            var lessPopularProduct = CreateProduct(
                "Less popular", "less-popular", "LESS", fallbackType, ProductStatus.Active,
                "https://cdn.example.test/less-popular.webp");
            var popularProduct = CreateProduct(
                "Popular", "popular", "POPULAR", fallbackType, ProductStatus.Active,
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
                draftProduct);
            await context.SaveChangesAsync();

            return new ShowcaseSeed(ownImageType.Id, fallbackType.Id, noImageType.Id);
        }

        // Burada HTTP fixture ürününü kategori ilişkisi ve isteğe bağlı ana görselle oluşturuyorum.
        private static Product CreateProduct(
            string title,
            string url,
            string sku,
            ProductType productType,
            ProductStatus status,
            string? imageUrl = null)
        {
            var product = new Product(
                title,
                url,
                $"{sku}-MAIN",
                typeId: productType.Id,
                status: status);
            if (imageUrl is not null)
            {
                product.Images.Add(new ProductImage(product, imageUrl, isMain: true));
            }

            return product;
        }

        // Burada test hostuyla birlikte açık SQLite bağlantısını kapatıyorum.
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection.Dispose();
            }
        }
    }

    // Burada HTTP fixture'ındaki public kategori kimliklerini taşıyorum.
    private sealed record ShowcaseSeed(
        Guid OwnImageTypeId,
        Guid FallbackTypeId,
        Guid NoImageTypeId);

    private sealed class CommandCounterInterceptor : DbCommandInterceptor
    {
        public int ReaderCommandCount { get; private set; }

        // Burada gerçek HTTP isteğinden önce ilişkisel reader komut sayacını sıfırlıyorum.
        public void Reset()
        {
            ReaderCommandCount = 0;
        }

        // Burada endpoint komutlarını sayarak kategori başına ek sorgu oluşmadığını doğruluyorum.
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
