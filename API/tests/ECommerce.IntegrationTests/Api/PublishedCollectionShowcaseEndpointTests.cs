using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Application.Collections.Dtos;
using ECommerce.Application.Common.Models;
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

public sealed class PublishedCollectionShowcaseEndpointTests
{
    // Burada anonim public endpointin özel görsel, fallback, null ve eleme davranışını gerçek HTTP hattında doğruluyorum.
    [Fact]
    public async Task Published_Collections_Should_Return_Showcase_Cards_Through_Real_Http_Pipeline()
    {
        await using var scenario = await ShowcaseApiScenario.CreateAsync();

        using var response = await scenario.Client.GetAsync(
            "/api/collections/published?PageNumber=1&PageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<
            PagedResult<PublishedCollectionShowcaseItemDto>>();
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(3);
        result.Items.Select(item => item.Id).Should().Equal(
            scenario.OwnImageCollectionId,
            scenario.FallbackCollectionId,
            scenario.NoImageCollectionId);
        result.Items[0].ImageUrl.Should().Be("https://cdn.example.test/collection.webp");
        result.Items[1].ImageUrl.Should().Be("https://cdn.example.test/fallback-main.webp");
        result.Items[1].ProductCount.Should().Be(1);
        result.Items[2].ImageUrl.Should().BeNull();
        scenario.CommandCounter.ReaderCommandCount.Should().Be(3);
    }

    // Burada geçersiz vitrin sayfalamasının ortak 400 ProblemDetails gövdesini ürettiğini doğruluyorum.
    [Fact]
    public async Task Published_Collections_Should_Return_Validation_Problem_For_Invalid_Page_Size()
    {
        await using var scenario = await ShowcaseApiScenario.CreateAsync();

        using var response = await scenario.Client.GetAsync(
            "/api/collections/published?PageNumber=1&PageSize=101");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().Should().Be("bad_request");
        problem.RootElement.GetProperty("status").GetInt32().Should().Be(400);
    }

    // Burada OpenAPI'nin anonim güvenlik, sayfalama ve nullable etkili görsel sözleşmesini yayınladığını doğruluyorum.
    [Fact]
    public async Task OpenApi_Should_Document_Published_Collection_Showcase_Contract()
    {
        await using var scenario = await ShowcaseApiScenario.CreateAsync();

        using var response = await scenario.Client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var operation = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/collections/published")
            .GetProperty("get");
        operation.GetProperty("security").GetArrayLength().Should().Be(0);
        operation.GetProperty("parameters")
            .EnumerateArray()
            .Select(parameter => parameter.GetProperty("name").GetString())
            .Should().BeEquivalentTo(["PageNumber", "PageSize"]);

        var schema = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("PublishedCollectionShowcaseItemDto");
        schema.GetProperty("required")
            .EnumerateArray()
            .Select(property => property.GetString())
            .Should().Contain(["id", "name", "url", "productCount", "isFeatured", "displayOrder"]);
        var imageUrl = schema.GetProperty("properties").GetProperty("imageUrl");
        imageUrl.GetProperty("nullable").GetBoolean().Should().BeTrue();
        imageUrl.GetProperty("maxLength").GetInt32().Should().Be(500);
    }

    private sealed class ShowcaseApiScenario : IAsyncDisposable
    {
        private readonly ShowcaseApiFactory _factory;

        public HttpClient Client { get; }
        public Guid OwnImageCollectionId { get; }
        public Guid FallbackCollectionId { get; }
        public Guid NoImageCollectionId { get; }
        public CommandCounterInterceptor CommandCounter { get; }

        // Burada HTTP client, host ve beklenen koleksiyon kimliklerini aynı yaşam döngüsünde saklıyorum.
        private ShowcaseApiScenario(
            ShowcaseApiFactory factory,
            HttpClient client,
            ShowcaseSeed seed)
        {
            _factory = factory;
            Client = client;
            OwnImageCollectionId = seed.OwnImageCollectionId;
            FallbackCollectionId = seed.FallbackCollectionId;
            NoImageCollectionId = seed.NoImageCollectionId;
            CommandCounter = factory.CommandCounter;
        }

        // Burada izole API hostunu başlatıp public koleksiyon vitrini verisini hazırlıyorum.
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
            builder.UseSetting("Jwt:Issuer", "ECommerce.ShowcaseIntegrationTests");
            builder.UseSetting("Jwt:Audience", "ECommerce.ShowcaseIntegrationTests.Client");
            builder.UseSetting("Jwt:SecretKey", "showcase-integration-test-secret-key-at-least-32-bytes");
            builder.UseSetting(
                "DataProtection:KeyRingPath",
                Path.Combine(Path.GetTempPath(), "ECommerce.ShowcaseIntegrationTests.DataProtection"));
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

        // Burada endpointin özel görsel, fallback, null ve public eleme sonuçlarını üretecek veriyi ekliyorum.
        public async Task<ShowcaseSeed> InitializeAndSeedAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync();

            var ownImageCollection = new Collection(
                "Own image", "own-image", displayOrder: 0,
                imageUrl: "https://cdn.example.test/collection.webp");
            var fallbackCollection = new Collection("Fallback", "fallback", displayOrder: 1);
            var noImageCollection = new Collection("No image", "no-image", displayOrder: 2);
            var inactiveCollection = new Collection("Inactive", "inactive", isActive: false, displayOrder: 3);
            var emptyCollection = new Collection("Empty", "empty", displayOrder: 4);
            var draftOnlyCollection = new Collection("Draft", "draft", displayOrder: 5);

            var ownProduct = CreateProduct(
                "Own product", "own-product", "OWN", ownImageCollection, ProductStatus.Active,
                "https://cdn.example.test/own-product.webp");
            var fallbackProduct = CreateProduct(
                "Fallback product", "fallback-product", "FALLBACK", fallbackCollection, ProductStatus.Active,
                "https://cdn.example.test/fallback-main.webp");
            var noImageProduct = CreateProduct(
                "No image product", "no-image-product", "NO-IMAGE", noImageCollection, ProductStatus.Active);
            var inactiveCollectionProduct = CreateProduct(
                "Inactive collection product", "inactive-collection-product", "INACTIVE-COLLECTION",
                inactiveCollection, ProductStatus.Active, "https://cdn.example.test/inactive.webp");
            var draftProduct = CreateProduct(
                "Draft product", "draft-product", "DRAFT", draftOnlyCollection, ProductStatus.Draft,
                "https://cdn.example.test/draft.webp");

            context.AddRange(
                ownImageCollection,
                fallbackCollection,
                noImageCollection,
                inactiveCollection,
                emptyCollection,
                draftOnlyCollection,
                ownProduct,
                fallbackProduct,
                noImageProduct,
                inactiveCollectionProduct,
                draftProduct);
            await context.SaveChangesAsync();

            return new ShowcaseSeed(
                ownImageCollection.Id,
                fallbackCollection.Id,
                noImageCollection.Id);
        }

        // Burada HTTP fixture ürününü koleksiyon ilişkisi ve isteğe bağlı ana görselle oluşturuyorum.
        private static Product CreateProduct(
            string title,
            string url,
            string sku,
            Collection collection,
            ProductStatus status,
            string? imageUrl = null)
        {
            var product = new Product(title, url, $"{sku}-MAIN", status: status);
            product.ProductCollections.Add(new ProductCollection(product, collection.Id));
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

    // Burada HTTP fixture'ındaki public koleksiyon kimliklerini taşıyorum.
    private sealed record ShowcaseSeed(
        Guid OwnImageCollectionId,
        Guid FallbackCollectionId,
        Guid NoImageCollectionId);

    private sealed class CommandCounterInterceptor : DbCommandInterceptor
    {
        public int ReaderCommandCount { get; private set; }

        // Burada gerçek HTTP isteğinden önce ilişkisel reader komut sayacını sıfırlıyorum.
        public void Reset()
        {
            ReaderCommandCount = 0;
        }

        // Burada endpoint boyunca çalışan reader komutlarını sayarak N+1 oluşmadığını doğruluyorum.
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
