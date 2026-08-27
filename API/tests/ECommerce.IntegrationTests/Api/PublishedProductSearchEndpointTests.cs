using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Search;
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

public sealed class PublishedProductSearchEndpointTests
{
    // Burada suggestion endpointinin tek SQL, COUNT olmadan Limit+1 ve nullable görsel sözleşmesini doğruluyorum.
    [Fact]
    public async Task Suggestions_Should_Use_One_Command_Without_Count_And_Return_HasMore()
    {
        await using var scenario = await SearchScenario.CreateAsync();
        scenario.Factory.CommandCounter.Reset();

        using var response = await scenario.Client.GetAsync(
            "/api/products/published/search-suggestions?Query=kolye&Limit=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PublishedProductSearchSuggestionsDto>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.HasMore.Should().BeTrue();
        result.Items.Should().OnlyContain(item => !string.IsNullOrWhiteSpace(item.Url));
        scenario.Factory.CommandCounter.ReaderCommandCount.Should().Be(1);
        scenario.Factory.CommandCounter.Commands.Should().NotContain(command =>
            command.Contains("COUNT(", StringComparison.OrdinalIgnoreCase));
    }

    // Burada exact, prefix ve contains başlık eşleşmelerinin sırasını SQL sonucu üzerinden doğruluyorum.
    [Fact]
    public async Task Suggestions_Should_Order_Exact_Prefix_And_Contains_Title_Matches()
    {
        await using var scenario = await SearchScenario.CreateAsync();

        var result = await scenario.Client.GetFromJsonAsync<PublishedProductSearchSuggestionsDto>(
            "/api/products/published/search-suggestions?Query=%C5%9F%C3%B6nil&Limit=10");

        result.Should().NotBeNull();
        result!.Items.Take(3).Select(item => item.Title).Should().Equal(
            "Şönil",
            "Şönil Başlangıç Kolye",
            "Lüks Şönil Kolye");
    }

    // Burada marka, tür, koleksiyon, etiket ve SKU alanlarının ortak arama dokümanında bulunabildiğini doğruluyorum.
    [Theory]
    [InlineData("markaanahtari", "Marka Alanı Ürünü")]
    [InlineData("turanahtari", "Tür Alanı Ürünü")]
    [InlineData("koleksiyonanahtari", "Koleksiyon Alanı Ürünü")]
    [InlineData("etiketanahtari", "Etiket Alanı Ürünü")]
    [InlineData("sku-ozel-42", "SKU Alanı Ürünü")]
    public async Task Suggestions_Should_Search_All_Documented_Fields(string query, string expectedTitle)
    {
        await using var scenario = await SearchScenario.CreateAsync();

        var result = await scenario.Client.GetFromJsonAsync<PublishedProductSearchSuggestionsDto>(
            $"/api/products/published/search-suggestions?Query={Uri.EscapeDataString(query)}&Limit=10");

        result!.Items.Should().ContainSingle(item => item.Title == expectedTitle);
    }

    // Burada çok kelimeli AND ve Türkçe harf normalizasyonunun aynı sorguda çalıştığını doğruluyorum.
    [Fact]
    public async Task Suggestions_Should_Apply_MultiToken_And_Turkish_Normalization()
    {
        await using var scenario = await SearchScenario.CreateAsync();

        var result = await scenario.Client.GetFromJsonAsync<PublishedProductSearchSuggestionsDto>(
            "/api/products/published/search-suggestions?Query=%20%20%C5%9E%C3%96N%C4%B0L%20%20%20kolye%20%20&Limit=10");

        result!.Items.Select(item => item.Title).Should().Contain(new[]
        {
            "Şönil Başlangıç Kolye",
            "Lüks Şönil Kolye"
        });
        result.Items.Should().NotContain(item => item.Title == "Şönil");
    }

    // Burada pasif/taslak ürünleri, görünürlük ayarlarını, canonical URL'yi ve null görseli doğruluyorum.
    [Fact]
    public async Task Suggestions_Should_Apply_Public_Visibility_And_Card_Semantics()
    {
        await using var scenario = await SearchScenario.CreateAsync();

        var result = await scenario.Client.GetFromJsonAsync<PublishedProductSearchSuggestionsDto>(
            "/api/products/published/search-suggestions?Query=gizlilik&Limit=10");

        result!.Items.Should().ContainSingle();
        var item = result.Items.Single();
        item.Title.Should().Be("Gizlilik Yayında");
        item.Url.Should().Be("backend-canonical-url");
        item.ImageUrl.Should().BeNull();
        item.ImageAlt.Should().BeNull();
        item.Price.Should().Be(120m);
        item.CompareAtPrice.Should().Be(150m);
        item.IsAvailable.Should().BeTrue();
    }

    // Burada stokta olmayan ve fiyat varyantı bulunmayan ürünlerin StoreSettings tercihleriyle SQL'de elendiğini doğruluyorum.
    [Fact]
    public async Task Suggestions_Should_Apply_StoreSettings_Visibility_Preferences()
    {
        await using var scenario = await SearchScenario.CreateAsync();
        var before = await scenario.Client.GetFromJsonAsync<PublishedProductSearchSuggestionsDto>(
            "/api/products/published/search-suggestions?Query=tercih&Limit=10");
        before!.Items.Should().HaveCount(2);

        await scenario.Factory.HideUnavailableProductsAsync();
        var after = await scenario.Client.GetFromJsonAsync<PublishedProductSearchSuggestionsDto>(
            "/api/products/published/search-suggestions?Query=tercih&Limit=10");

        after!.Items.Should().BeEmpty();
    }

    // Burada tam aramanın taxonomy filtresiyle AND çalıştığını ve explicit sıralamanın relevance'ı ezdiğini doğruluyorum.
    [Fact]
    public async Task Full_Search_Should_Combine_Taxonomy_Filter_And_Explicit_Sort()
    {
        await using var scenario = await SearchScenario.CreateAsync();
        scenario.Factory.CommandCounter.Reset();

        var path = $"/api/products/published?Search=kolye&BrandId={scenario.TargetBrandId:D}" +
            "&SortBy=3&Descending=true&PageNumber=1&PageSize=24";
        var result = await scenario.Client.GetFromJsonAsync<PagedResult<PublishedProductListItemDto>>(path);

        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(2);
        result.Items.Select(item => item.Title).Should().BeInDescendingOrder();
        scenario.Factory.CommandCounter.ReaderCommandCount.Should().Be(2);
    }

    // Burada kısa sorgu ile sınır dışı limitin ortak validation ProblemDetails cevabı ürettiğini doğruluyorum.
    [Theory]
    [InlineData("/api/products/published/search-suggestions?Query=a")]
    [InlineData("/api/products/published/search-suggestions?Query=ab&Limit=11")]
    public async Task Invalid_Suggestion_Request_Should_Return_Validation_Problem(string path)
    {
        await using var scenario = await SearchScenario.CreateAsync();

        using var response = await scenario.Client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().Should().Be("validation_error");
    }

    // Burada public arama politikasının IP başına 120 isteğin ardından standart 429 ProblemDetails döndürdüğünü doğruluyorum.
    [Fact]
    public async Task Suggestion_Rate_Limit_Should_Return_ProblemDetails()
    {
        await using var scenario = await SearchScenario.CreateAsync();
        for (var index = 0; index < 120; index++)
        {
            using var accepted = await scenario.Client.GetAsync(
                "/api/products/published/search-suggestions?Query=a");
            accepted.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        using var response = await scenario.Client.GetAsync(
            "/api/products/published/search-suggestions?Query=a");

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().Should().Be("rate_limit_exceeded");
    }

    // Burada OpenAPI'nin anonim güvenlik, sınırlar, Search alanı ve 200/400/429 cevaplarını yayımladığını doğruluyorum.
    [Fact]
    public async Task OpenApi_Should_Document_Public_Search_Contract()
    {
        await using var scenario = await SearchScenario.CreateAsync();

        using var response = await scenario.Client.GetAsync("/swagger/v1/swagger.json");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        var operation = paths.GetProperty("/api/products/published/search-suggestions").GetProperty("get");
        operation.GetProperty("security").GetArrayLength().Should().Be(0);
        operation.GetProperty("responses").TryGetProperty("200", out _).Should().BeTrue();
        operation.GetProperty("responses").TryGetProperty("400", out _).Should().BeTrue();
        operation.GetProperty("responses").TryGetProperty("429", out _).Should().BeTrue();
        var parameters = operation.GetProperty("parameters").EnumerateArray().ToList();
        var query = parameters.Single(parameter => parameter.GetProperty("name").GetString() == "Query");
        query.GetProperty("required").GetBoolean().Should().BeTrue();
        query.GetProperty("schema").GetProperty("minLength").GetInt32().Should().Be(2);
        query.GetProperty("schema").GetProperty("maxLength").GetInt32().Should().Be(100);
        var published = paths.GetProperty("/api/products/published").GetProperty("get");
        published.GetProperty("parameters").EnumerateArray()
            .Should().Contain(parameter => parameter.GetProperty("name").GetString() == "Search");
        foreach (var path in new[]
                 {
                     "/api/products/published",
                     "/api/products/by-collection/{collectionId}",
                     "/api/products/by-tag/{tagId}",
                     "/api/products/by-type/{typeId}",
                     "/api/products/by-brand/{brandId}"
                 })
        {
            var sortBy = paths.GetProperty(path).GetProperty("get").GetProperty("parameters")
                .EnumerateArray()
                .Single(parameter => parameter.GetProperty("name").GetString() == "SortBy");
            sortBy.GetProperty("description").GetString().Should().Contain("4 BestSelling");
        }

        document.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty("PublishedProductSortBy").GetProperty("enum")
            .EnumerateArray().Select(value => value.GetInt32())
            .Should().Equal(0, 1, 2, 3, 4);
    }

    private sealed class SearchScenario : IAsyncDisposable
    {
        public SearchApiFactory Factory { get; }
        public HttpClient Client { get; }
        public Guid TargetBrandId { get; }

        // Burada arama HTTP hostunu ve test sınıflandırma kimliğini birlikte saklıyorum.
        private SearchScenario(SearchApiFactory factory, HttpClient client, Guid targetBrandId)
        {
            Factory = factory;
            Client = client;
            TargetBrandId = targetBrandId;
        }

        // Burada izole API hostunu başlatıp arama dokümanlarıyla birlikte test kataloğunu hazırlıyorum.
        public static async Task<SearchScenario> CreateAsync()
        {
            var factory = new SearchApiFactory();
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });
            var brandId = await factory.InitializeAndSeedAsync();
            factory.CommandCounter.Reset();
            return new SearchScenario(factory, client, brandId);
        }

        // Burada HTTP client ve test hostu yaşam döngüsünü birlikte kapatıyorum.
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await Factory.DisposeAsync();
        }
    }

    private sealed class SearchApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:;Foreign Keys=True");
        public CommandCaptureInterceptor CommandCounter { get; } = new();

        // Burada HTTP testleri boyunca ortak ilişkisel SQLite bağlantısını açık tutuyorum.
        public SearchApiFactory()
        {
            _connection.Open();
        }

        // Burada gerçek API hattını SQLite ve SQL komut sayacıyla çalıştırıyorum.
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "unused-by-test-override");
            builder.UseSetting("Jwt:Issuer", "ECommerce.SearchIntegrationTests");
            builder.UseSetting("Jwt:Audience", "ECommerce.SearchIntegrationTests.Client");
            builder.UseSetting("Jwt:SecretKey", "search-integration-test-secret-key-at-least-32-bytes");
            builder.UseSetting("DataProtection:KeyRingPath", Path.Combine(Path.GetTempPath(), "ECommerce.SearchTests.Keys"));
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

        // Burada relevance, taxonomy, görünürlük, payload ve hasMore senaryolarını üreten kataloğu ekliyorum.
        public async Task<Guid> InitializeAndSeedAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync();
            var targetBrand = new Brand("Hedef Marka", "hedef-marka");
            var searchBrand = new Brand("MarkaAnahtari", "marka-anahtari");
            var normalBrand = new Brand("Normal Marka", "normal-marka");
            var normalType = new ProductType("Normal Tür");
            var searchType = new ProductType("TurAnahtari");
            var searchCollection = new Collection("KoleksiyonAnahtari", "koleksiyon-anahtari");
            var searchTag = new Tag("EtiketAnahtari", "etiket-anahtari");
            context.AddRange(targetBrand, searchBrand, normalBrand, normalType, searchType, searchCollection, searchTag);
            await context.SaveChangesAsync();

            var seeds = new[]
            {
                NewSeed("Şönil", "sonil-exact", "EXACT", targetBrand, normalType, image: "https://cdn.test/exact.jpg"),
                NewSeed("Şönil Başlangıç Kolye", "sonil-prefix", "PREFIX", targetBrand, normalType),
                NewSeed("Lüks Şönil Kolye", "sonil-contains", "CONTAINS", normalBrand, normalType),
                NewSeed("Zirve Kolye", "zirve-kolye", "ZIRVE", targetBrand, normalType),
                NewSeed("Marka Alanı Ürünü", "brand-field", "BRAND", searchBrand, normalType),
                NewSeed("Tür Alanı Ürünü", "type-field", "TYPE", normalBrand, searchType),
                NewSeed("Koleksiyon Alanı Ürünü", "collection-field", "COLLECTION", normalBrand, normalType, searchCollection),
                NewSeed("Etiket Alanı Ürünü", "tag-field", "TAG", normalBrand, normalType, tag: searchTag),
                NewSeed("SKU Alanı Ürünü", "sku-field", "SKU-OZEL-42", normalBrand, normalType),
                NewSeed("Gizlilik Yayında", "backend-canonical-url", "VISIBLE", normalBrand, normalType, compareAtPrice: 150m),
                NewSeed("Gizlilik Taslak", "draft", "DRAFT", normalBrand, normalType, status: ProductStatus.Draft),
                NewSeed("Gizlilik Pasif", "inactive", "INACTIVE", normalBrand, normalType, isActive: false),
                NewSeed("Tercih Stoksuz", "preference-stock", "PREF-STOCK", normalBrand, normalType, stock: 0),
                NewSeed("Tercih Fiyatsız", "preference-price", "PREF-PRICE", normalBrand, normalType, withVariant: false),
                NewSeed("Ek Kolye Bir", "extra-kolye-1", "EXTRA1", normalBrand, normalType),
                NewSeed("Ek Kolye İki", "extra-kolye-2", "EXTRA2", normalBrand, normalType)
            };
            context.Products.AddRange(seeds.Select(seed => seed.Product));
            await context.SaveChangesAsync();
            foreach (var seed in seeds)
            {
                var document = ProductSearchIndexBuilder.CreateDocument(
                    seed.Product.Id,
                    seed.Product.Title,
                    seed.Product.MainSku,
                    seed.Brand.Name,
                    seed.Type.Name,
                    seed.Collection is null ? [] : [seed.Collection.Name],
                    seed.Tag is null ? [] : [seed.Tag.Name]);
                context.ProductSearchDocuments.Add(document);
                context.ProductSearchGrams.AddRange(ProductSearchIndexBuilder.CreateGrams(document));
            }
            await context.SaveChangesAsync();
            return targetBrand.Id;
        }

        // Burada test mağaza ayarlarını stokta olmayan ve fiyatsız ürünleri gizleyecek biçimde güncelliyorum.
        public async Task HideUnavailableProductsAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var settings = await context.StoreSettings.SingleAsync();
            settings.UpdateStorefront(
                StorefrontStatus.Active,
                null,
                false,
                false,
                StorefrontProductSort.Newest,
                true,
                true,
                false,
                5);
            await context.SaveChangesAsync();
        }

        // Burada tek aktif varyantlı ve isteğe bağlı taxonomy/görselli test ürününü oluşturuyorum.
        private static SearchSeed NewSeed(
            string title,
            string url,
            string sku,
            Brand brand,
            ProductType type,
            Collection? collection = null,
            Tag? tag = null,
            string? image = null,
            decimal? compareAtPrice = null,
            ProductStatus status = ProductStatus.Active,
            bool isActive = true,
            int stock = 5,
            bool withVariant = true)
        {
            var product = new Product(title, url, sku, type.Id, brand.Id, status: status, isActive: isActive);
            if (withVariant)
            {
                product.Variants.Add(new ProductVariant(product, "Standart", $"{sku}-V", 120m, stock, compareAtPrice));
            }
            if (collection is not null)
            {
                product.ProductCollections.Add(new ProductCollection(product, collection.Id));
            }
            if (tag is not null)
            {
                product.ProductTags.Add(new ProductTag(product, tag.Id));
            }
            if (image is not null)
            {
                product.Images.Add(new ProductImage(product, image, isMain: true, altText: title));
            }
            return new SearchSeed(product, brand, type, collection, tag);
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

    // Burada arama dokümanı üretmek için ürünün sınıflandırma isimlerini birlikte taşıyorum.
    private sealed record SearchSeed(
        Product Product,
        Brand Brand,
        ProductType Type,
        Collection? Collection,
        Tag? Tag);

    private sealed class CommandCaptureInterceptor : DbCommandInterceptor
    {
        public int ReaderCommandCount { get; private set; }
        public List<string> Commands { get; } = [];

        // Burada gerçek HTTP isteğinden önce komut sayısı ve SQL metinlerini sıfırlıyorum.
        public void Reset()
        {
            ReaderCommandCount = 0;
            Commands.Clear();
        }

        // Burada endpointin reader komutlarını ve COUNT kullanıp kullanmadığını kaydediyorum.
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ReaderCommandCount++;
            Commands.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
