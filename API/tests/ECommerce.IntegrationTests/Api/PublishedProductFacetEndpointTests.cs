using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ECommerce.IntegrationTests.Api;

public sealed class PublishedProductFacetEndpointTests
{
    // Burada üç ayrı public endpointin seçenek başına ek istek olmadan adetli facet listesi döndürdüğünü doğruluyorum.
    [Fact]
    public async Task Public_Facet_Endpoints_Should_Return_Filtered_Counts_Through_Real_Http_Pipeline()
    {
        await using var scenario = await FacetApiScenario.CreateAsync();

        var brands = await GetFacetsAsync(
            scenario.Client,
            $"/api/products/published/facets/brands?typeId={scenario.ShirtTypeId:D}" +
            $"&brandId={scenario.BetaBrandId:D}&collectionId={scenario.SummerCollectionId:D}&tagId={scenario.SaleTagId:D}");
        var collections = await GetFacetsAsync(
            scenario.Client,
            $"/api/products/published/facets/collections?typeId={scenario.ShirtTypeId:D}" +
            $"&brandId={scenario.AlphaBrandId:D}&collectionId={scenario.WinterCollectionId:D}&tagId={scenario.SaleTagId:D}");
        var productTypes = await GetFacetsAsync(
            scenario.Client,
            $"/api/products/published/facets/product-types?typeId={scenario.ShoeTypeId:D}" +
            $"&brandId={scenario.AlphaBrandId:D}&collectionId={scenario.SummerCollectionId:D}&tagId={scenario.SaleTagId:D}");

        brands.Should().HaveCount(2);
        brands.Should().OnlyContain(facet => facet.ProductCount == 1);
        collections.Should().ContainSingle(facet =>
            facet.Id == scenario.SummerCollectionId && facet.ProductCount == 1);
        productTypes.Should().ContainSingle(facet =>
            facet.Id == scenario.ShirtTypeId && facet.ProductCount == 1);
    }

    // Burada OpenAPI'nin üç route'u, opsiyonel filtreleri, zorunlu response alanlarını ve örneği yayımladığını doğruluyorum.
    [Fact]
    public async Task OpenApi_Should_Document_Facet_Endpoints_Query_Contract_And_Response_Example()
    {
        await using var scenario = await FacetApiScenario.CreateAsync();

        using var response = await scenario.Client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        foreach (var path in new[]
        {
            "/api/products/published/facets/brands",
            "/api/products/published/facets/collections",
            "/api/products/published/facets/product-types"
        })
        {
            var operation = paths.GetProperty(path).GetProperty("get");
            var parameters = operation.GetProperty("parameters").EnumerateArray().ToList();
            parameters.Select(parameter => parameter.GetProperty("name").GetString())
                .Should().BeEquivalentTo(["TypeId", "BrandId", "CollectionId", "TagId"]);
            foreach (var parameter in parameters)
            {
                if (parameter.TryGetProperty("required", out var required))
                {
                    required.GetBoolean().Should().BeFalse();
                }

                var parameterSchema = parameter.GetProperty("schema");
                parameterSchema.GetProperty("type").GetString().Should().Be("string");
                parameterSchema.GetProperty("format").GetString().Should().Be("uuid");
                parameterSchema.GetProperty("nullable").GetBoolean().Should().BeTrue();
            }

            var jsonResponse = operation
                .GetProperty("responses")
                .GetProperty("200")
                .GetProperty("content")
                .GetProperty("application/json");
            jsonResponse.GetProperty("schema").GetProperty("type").GetString().Should().Be("array");
            jsonResponse.GetProperty("example").GetArrayLength().Should().BeGreaterThan(0);
        }

        var facetSchema = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("PublishedProductFacetItemDto");
        facetSchema.GetProperty("required")
            .EnumerateArray()
            .Select(property => property.GetString())
            .Should().BeEquivalentTo(["id", "name", "productCount"]);
    }

    // Burada boş GUID facet filtresinin Application doğrulamasından 400 Problem Details ürettiğini doğruluyorum.
    [Fact]
    public async Task Empty_Facet_Filter_Id_Should_Return_Validation_Problem()
    {
        await using var scenario = await FacetApiScenario.CreateAsync();

        using var response = await scenario.Client.GetAsync(
            $"/api/products/published/facets/brands?BrandId={Guid.Empty:D}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().Should().Be("validation_error");
        problem.RootElement.GetProperty("errors").TryGetProperty("BrandId", out _).Should().BeTrue();
    }

    // Burada facet endpoint cevabını gerçek HTTP durum kodu ve JSON sözleşmesiyle okuyorum.
    private static async Task<IReadOnlyList<PublishedProductFacetItemDto>> GetFacetsAsync(
        HttpClient client,
        string path)
    {
        using var response = await client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var facets = await response.Content.ReadFromJsonAsync<IReadOnlyList<PublishedProductFacetItemDto>>();
        facets.Should().NotBeNull();
        return facets!;
    }

    private sealed class FacetApiScenario : IAsyncDisposable
    {
        private readonly FacetApiFactory _factory;

        public HttpClient Client { get; }
        public Guid ShirtTypeId { get; }
        public Guid ShoeTypeId { get; }
        public Guid AlphaBrandId { get; }
        public Guid BetaBrandId { get; }
        public Guid SummerCollectionId { get; }
        public Guid WinterCollectionId { get; }
        public Guid SaleTagId { get; }

        // Burada HTTP client, host ve facet filtre kimliklerini tek test yaşam döngüsünde saklıyorum.
        private FacetApiScenario(
            FacetApiFactory factory,
            HttpClient client,
            FacetSeed seed)
        {
            _factory = factory;
            Client = client;
            ShirtTypeId = seed.ShirtTypeId;
            ShoeTypeId = seed.ShoeTypeId;
            AlphaBrandId = seed.AlphaBrandId;
            BetaBrandId = seed.BetaBrandId;
            SummerCollectionId = seed.SummerCollectionId;
            WinterCollectionId = seed.WinterCollectionId;
            SaleTagId = seed.SaleTagId;
        }

        // Burada izole API hostunu başlatıp yayımlanmış facet senaryosunu veritabanına hazırlıyorum.
        public static async Task<FacetApiScenario> CreateAsync()
        {
            var factory = new FacetApiFactory();
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });
            var seed = await factory.InitializeAndSeedAsync();
            return new FacetApiScenario(factory, client, seed);
        }

        // Burada test HTTP clientını ve hostunu birlikte kapatıyorum.
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _factory.DisposeAsync();
        }
    }

    private sealed class FacetApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:;Foreign Keys=True");

        // Burada HTTP testlerinin ortak ilişkisel SQLite bağlantısını açık tutuyorum.
        public FacetApiFactory()
        {
            _connection.Open();
        }

        // Burada gerçek API hattını SQLite ile çalıştırıp arka plan servislerini testten çıkarıyorum.
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "unused-by-test-override");
            builder.UseSetting("Jwt:Issuer", "ECommerce.FacetIntegrationTests");
            builder.UseSetting("Jwt:Audience", "ECommerce.FacetIntegrationTests.Client");
            builder.UseSetting("Jwt:SecretKey", "facet-integration-test-secret-key-at-least-32-bytes");
            builder.UseSetting(
                "DataProtection:KeyRingPath",
                Path.Combine(Path.GetTempPath(), "ECommerce.FacetIntegrationTests.DataProtection"));
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

                services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
            });
        }

        // Burada filtrelerin öz-dışlama ve çoklu boyut davranışını gösterecek katalog verisini ekliyorum.
        public async Task<FacetSeed> InitializeAndSeedAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync();

            var shirtType = new ProductType("Shirt");
            var shoeType = new ProductType("Shoe");
            var alphaBrand = new Brand("Alpha", "alpha");
            var betaBrand = new Brand("Beta", "beta");
            var emptyBrand = new Brand("Empty", "empty");
            var summerCollection = new Collection("Summer", "summer");
            var winterCollection = new Collection("Winter", "winter");
            var saleTag = new Tag("Sale", "sale");
            var alphaShirt = CreateProduct(
                "Alpha shirt", "alpha-shirt", "ALPHA-SHIRT", shirtType, alphaBrand, summerCollection, saleTag);
            var betaShirt = CreateProduct(
                "Beta shirt", "beta-shirt", "BETA-SHIRT", shirtType, betaBrand, summerCollection, saleTag);
            var alphaShoe = CreateProduct(
                "Alpha shoe", "alpha-shoe", "ALPHA-SHOE", shoeType, alphaBrand, winterCollection, saleTag);
            var draft = CreateProduct(
                "Draft", "draft", "DRAFT", shirtType, alphaBrand, summerCollection, saleTag, ProductStatus.Draft);

            context.AddRange(
                shirtType, shoeType,
                alphaBrand, betaBrand, emptyBrand,
                summerCollection, winterCollection,
                saleTag,
                alphaShirt, betaShirt, alphaShoe, draft);
            await context.SaveChangesAsync();

            return new FacetSeed(
                shirtType.Id,
                shoeType.Id,
                alphaBrand.Id,
                betaBrand.Id,
                summerCollection.Id,
                winterCollection.Id,
                saleTag.Id);
        }

        // Burada HTTP fixture ürünü için tür, marka, koleksiyon ve etiket ilişkilerini kuruyorum.
        private static Product CreateProduct(
            string title,
            string url,
            string sku,
            ProductType type,
            Brand brand,
            Collection collection,
            Tag tag,
            ProductStatus status = ProductStatus.Active)
        {
            var product = new Product(
                title,
                url,
                $"{sku}-MAIN",
                type.Id,
                brand.Id,
                status: status);
            product.ProductCollections.Add(new ProductCollection(product, collection.Id));
            product.ProductTags.Add(new ProductTag(product, tag.Id));
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

    // Burada HTTP fixture'ında kullanılacak sınıflandırma kimliklerini taşıyorum.
    private sealed record FacetSeed(
        Guid ShirtTypeId,
        Guid ShoeTypeId,
        Guid AlphaBrandId,
        Guid BetaBrandId,
        Guid SummerCollectionId,
        Guid WinterCollectionId,
        Guid SaleTagId);
}
