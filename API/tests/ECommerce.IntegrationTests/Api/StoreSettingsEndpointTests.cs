using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Data.Common;
using System.Text.Encodings.Web;
using System.Text.Json;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.IntegrationTests.Api;

public sealed class StoreSettingsEndpointTests
{
    // Burada public ayarların anonim erişildiğini, gizli iletişim ve yasal alan sızdırmadığını doğruluyorum.
    [Fact]
    public async Task Public_Get_Should_Be_Anonymous_And_Exclude_Sensitive_Fields()
    {
        await using var factory = new StoreSettingsApiFactory(TestIdentity.None);
        using var client = factory.CreateClient(CreateClientOptions());
        await factory.InitializeAsync();

        using var response = await client.GetAsync("/api/store-settings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.GetProperty("supportEmail").ValueKind.Should().Be(JsonValueKind.Null);
        root.TryGetProperty("taxNumber", out _).Should().BeFalse();
        root.TryGetProperty("nationalIdentityNumber", out _).Should().BeFalse();
        root.TryGetProperty("mersisNumber", out _).Should().BeFalse();
        root.TryGetProperty("tradeRegistryNumber", out _).Should().BeFalse();
        root.TryGetProperty("concurrencyToken", out _).Should().BeFalse();
    }

    // Burada admin GET için anonim kullanıcıda 401, müşteri rolünde 403 ve yöneticide 200 ayrımını doğruluyorum.
    [Fact]
    public async Task Admin_Get_Should_Enforce_Authentication_And_Admin_Role()
    {
        await using var anonymousFactory = new StoreSettingsApiFactory(TestIdentity.None);
        using var anonymousClient = anonymousFactory.CreateClient(CreateClientOptions());
        await anonymousFactory.InitializeAsync();
        using var anonymousResponse = await anonymousClient.GetAsync("/api/store-settings/admin");
        anonymousResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertProblemCodeAsync(anonymousResponse, "authentication_required");

        await using var customerFactory = new StoreSettingsApiFactory(TestIdentity.Customer);
        using var customerClient = customerFactory.CreateClient(CreateClientOptions());
        await customerFactory.InitializeAsync();
        using var customerResponse = await customerClient.GetAsync("/api/store-settings/admin");
        customerResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await AssertProblemCodeAsync(customerResponse, "forbidden");

        await using var adminFactory = new StoreSettingsApiFactory(TestIdentity.Admin);
        using var adminClient = adminFactory.CreateClient(CreateClientOptions());
        await adminFactory.InitializeAsync();
        using var adminResponse = await adminClient.GetAsync("/api/store-settings/admin");
        adminResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(adminResponse)).GetProperty("concurrencyToken").GetGuid().Should().NotBeEmpty();
    }

    // Burada kimlik ve iletişim PUT'larının bölüm izolasyonunu, görünürlük kararını ve eski token 409 davranışını doğruluyorum.
    [Fact]
    public async Task Section_Puts_Should_Preserve_Other_Sections_And_Reject_Stale_Token()
    {
        await using var factory = new StoreSettingsApiFactory(TestIdentity.Admin);
        using var client = factory.CreateClient(CreateClientOptions());
        await factory.InitializeAsync();
        var initial = await GetAdminAsync(client);
        var initialToken = initial.GetProperty("concurrencyToken").GetGuid();

        using var contactResponse = await client.PutAsJsonAsync("/api/store-settings/contact", new
        {
            supportEmail = "support@example.com",
            supportPhone = "+90 555 111 22 33",
            whatsappNumber = "+90 555 444 55 66",
            contactAddress = "İstanbul",
            workingHours = "09:00-18:00",
            mapUrl = "https://maps.example.com/store",
            showSupportEmail = false,
            showSupportPhone = true,
            showWhatsapp = true,
            showContactAddress = true,
            showWorkingHours = true,
            showMap = true,
            expectedConcurrencyToken = initialToken
        });
        contactResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var contact = await ReadJsonAsync(contactResponse);
        var contactToken = contact.GetProperty("concurrencyToken").GetGuid();
        contactToken.Should().NotBe(initialToken);

        using var identityResponse = await client.PutAsJsonAsync("/api/store-settings/identity", new
        {
            displayName = "  Yeni Mağaza  ",
            shortDescription = "Açıklama",
            logoUrl = "https://cdn.example.com/logo.png",
            darkLogoUrl = (string?)null,
            faviconUrl = (string?)null,
            defaultShareImageUrl = (string?)null,
            expectedConcurrencyToken = contactToken
        });
        identityResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var identity = await ReadJsonAsync(identityResponse);
        identity.GetProperty("displayName").GetString().Should().Be("Yeni Mağaza");
        identity.GetProperty("supportEmail").GetString().Should().Be("support@example.com");
        var identityToken = identity.GetProperty("concurrencyToken").GetGuid();
        identityToken.Should().NotBe(contactToken);

        using var staleResponse = await client.PutAsJsonAsync("/api/store-settings/identity", new
        {
            displayName = "Eski İstek",
            shortDescription = (string?)null,
            logoUrl = (string?)null,
            darkLogoUrl = (string?)null,
            faviconUrl = (string?)null,
            defaultShareImageUrl = (string?)null,
            expectedConcurrencyToken = contactToken
        });
        staleResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await AssertProblemCodeAsync(staleResponse, "concurrency_conflict");

        using var publicResponse = await client.GetAsync("/api/store-settings");
        var publicSettings = await ReadJsonAsync(publicResponse);
        publicSettings.GetProperty("supportEmail").ValueKind.Should().Be(JsonValueKind.Null);
        publicSettings.GetProperty("supportPhone").GetString().Should().Be("+90 555 111 22 33");
        (await GetAdminAsync(client)).GetProperty("displayName").GetString().Should().Be("Yeni Mağaza");
        identityToken.Should().NotBeEmpty();
    }

    // Burada aynı tokenla paralel iki güncellemeden yalnız birinin başarılı olduğunu doğruluyorum.
    [Fact]
    public async Task Parallel_Puts_With_Same_Token_Should_Allow_Only_One_Update()
    {
        await using var factory = new StoreSettingsApiFactory(TestIdentity.Admin);
        using var firstClient = factory.CreateClient(CreateClientOptions());
        using var secondClient = factory.CreateClient(CreateClientOptions());
        await factory.InitializeAsync();
        var token = (await GetAdminAsync(firstClient)).GetProperty("concurrencyToken").GetGuid();
        var firstRequest = CreateIdentityRequest("Birinci", token);
        var secondRequest = CreateIdentityRequest("İkinci", token);

        var responses = await Task.WhenAll(
            firstClient.PutAsJsonAsync("/api/store-settings/identity", firstRequest),
            secondClient.PutAsJsonAsync("/api/store-settings/identity", secondRequest));

        responses.Select(response => response.StatusCode)
            .Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.Conflict]);
        var conflict = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
        await AssertProblemCodeAsync(conflict, "concurrency_conflict");
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    // Burada veritabanı anahtarının ikinci singleton kaydını kabul etmediğini doğruluyorum.
    [Fact]
    public async Task Database_Should_Reject_A_Second_StoreSettings_Record()
    {
        await using var factory = new StoreSettingsApiFactory(TestIdentity.Admin);
        await factory.InitializeAsync();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.ChangeTracker.Clear();
        context.StoreSettings.Add(StoreSettings.CreateDefault());

        var action = async () => await context.SaveChangesAsync();

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    // Burada geçersiz e-posta, medya URL'si ve sosyal URL'nin 400 Problem Details ürettiğini doğruluyorum.
    [Fact]
    public async Task Invalid_Email_And_Urls_Should_Return_Validation_Problem()
    {
        await using var factory = new StoreSettingsApiFactory(TestIdentity.Admin);
        using var client = factory.CreateClient(CreateClientOptions());
        await factory.InitializeAsync();
        var token = (await GetAdminAsync(client)).GetProperty("concurrencyToken").GetGuid();

        using var contact = await client.PutAsJsonAsync("/api/store-settings/contact", new
        {
            supportEmail = "not-an-email",
            supportPhone = (string?)null,
            whatsappNumber = (string?)null,
            contactAddress = (string?)null,
            workingHours = (string?)null,
            mapUrl = "javascript:alert(1)",
            showSupportEmail = true,
            showSupportPhone = false,
            showWhatsapp = false,
            showContactAddress = false,
            showWorkingHours = false,
            showMap = false,
            expectedConcurrencyToken = token
        });
        contact.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertProblemCodeAsync(contact, "validation_error");

        using var identity = await client.PutAsJsonAsync(
            "/api/store-settings/identity",
            CreateIdentityRequest("Mağaza", token, "//cdn.example.com/logo.png"));
        identity.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var seo = await client.PutAsJsonAsync("/api/store-settings/seo", new
        {
            defaultTitle = "Başlık",
            titleTemplate = "%s | Mağaza",
            defaultDescription = (string?)null,
            defaultOpenGraphImageUrl = "ftp://cdn.example.com/og.png",
            allowIndexing = true,
            facebookUrl = "javascript:alert(1)",
            instagramUrl = (string?)null,
            tiktokUrl = (string?)null,
            youtubeUrl = (string?)null,
            xUrl = (string?)null,
            pinterestUrl = (string?)null,
            expectedConcurrencyToken = token
        });
        seo.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetAdminAsync(client)).GetProperty("concurrencyToken").GetGuid().Should().Be(token);
    }

    // Burada yasal bölüm PUT'unun admin cevabına yazıldığını fakat public sözleşmeye sızmadığını doğruluyorum.
    [Fact]
    public async Task Legal_Put_Should_Remain_Admin_Only_Data()
    {
        await using var factory = new StoreSettingsApiFactory(TestIdentity.Admin);
        using var client = factory.CreateClient(CreateClientOptions());
        await factory.InitializeAsync();
        var initial = await GetAdminAsync(client);
        var token = initial.GetProperty("concurrencyToken").GetGuid();

        using var response = await client.PutAsJsonAsync("/api/store-settings/legal", new
        {
            legalCompanyName = "Örnek Ticaret A.Ş.",
            taxOffice = "Merkez",
            taxNumber = "1234567890",
            nationalIdentityNumber = "12345678901",
            mersisNumber = "0123456789012345",
            tradeRegistryNumber = "TR-12345",
            country = "Türkiye",
            city = "İstanbul",
            district = "Kadıköy",
            addressLine = "Örnek Mahallesi",
            postalCode = "34000",
            expectedConcurrencyToken = token
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var admin = await ReadJsonAsync(response);
        admin.GetProperty("taxNumber").GetString().Should().Be("1234567890");
        admin.GetProperty("displayName").GetString().Should().Be(initial.GetProperty("displayName").GetString());
        admin.GetProperty("concurrencyToken").GetGuid().Should().NotBe(token);
        using var publicResponse = await client.GetAsync("/api/store-settings");
        var publicSettings = await ReadJsonAsync(publicResponse);
        publicSettings.TryGetProperty("legalCompanyName", out _).Should().BeFalse();
        publicSettings.TryGetProperty("taxNumber", out _).Should().BeFalse();
    }

    // Burada SEO güncellemesinin diğer bölümleri koruduğunu ve public nullable sosyal alanları doğru döndürdüğünü doğruluyorum.
    [Fact]
    public async Task Seo_Put_Should_Update_Public_Defaults_Without_Changing_Identity()
    {
        await using var factory = new StoreSettingsApiFactory(TestIdentity.Admin);
        using var client = factory.CreateClient(CreateClientOptions());
        await factory.InitializeAsync();
        await factory.SeedProductsAsync();
        var productSeoBefore = await factory.ReadProductSeoAsync("available");
        var initial = await GetAdminAsync(client);
        var displayName = initial.GetProperty("displayName").GetString();
        var token = initial.GetProperty("concurrencyToken").GetGuid();

        using var response = await client.PutAsJsonAsync("/api/store-settings/seo", new
        {
            defaultTitle = "Varsayılan Başlık",
            titleTemplate = "%s | Örnek",
            defaultDescription = "Varsayılan açıklama",
            defaultOpenGraphImageUrl = "https://cdn.example.com/og.png",
            allowIndexing = false,
            facebookUrl = "https://social.example.com/facebook",
            instagramUrl = (string?)null,
            tiktokUrl = (string?)null,
            youtubeUrl = (string?)null,
            xUrl = (string?)null,
            pinterestUrl = (string?)null,
            expectedConcurrencyToken = token
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await ReadJsonAsync(response);
        updated.GetProperty("displayName").GetString().Should().Be(displayName);
        updated.GetProperty("concurrencyToken").GetGuid().Should().NotBe(token);
        using var publicResponse = await client.GetAsync("/api/store-settings");
        var publicSettings = await ReadJsonAsync(publicResponse);
        publicSettings.GetProperty("allowIndexing").GetBoolean().Should().BeFalse();
        publicSettings.GetProperty("facebookUrl").GetString().Should().Be("https://social.example.com/facebook");
        publicSettings.GetProperty("instagramUrl").ValueKind.Should().Be(JsonValueKind.Null);
        (await factory.ReadProductSeoAsync("available")).Should().Be(productSeoBefore);
    }

    // Burada storefront tercihlerini stok/fiyat filtresi, doğru totalCount, sıralama ve stok özetine uyguluyorum.
    [Fact]
    public async Task Storefront_Preferences_Should_Change_Published_Product_Query_Before_Paging()
    {
        await using var factory = new StoreSettingsApiFactory(TestIdentity.Admin);
        using var client = factory.CreateClient(CreateClientOptions());
        await factory.InitializeAsync();
        await factory.SeedProductsAsync();
        var token = (await GetAdminAsync(client)).GetProperty("concurrencyToken").GetGuid();

        using var update = await client.PutAsJsonAsync("/api/store-settings/storefront", new
        {
            status = 1,
            statusMessage = "Bakım sürüyor",
            showOutOfStockProducts = false,
            showProductsWithoutPrice = false,
            defaultProductSort = 3,
            defaultProductSortDescending = false,
            showCompareAtPrice = false,
            showStockWarning = true,
            lowStockThreshold = 3,
            expectedConcurrencyToken = token
        });
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedToken = (await ReadJsonAsync(update)).GetProperty("concurrencyToken").GetGuid();

        using var filteredResponse = await client.GetAsync("/api/products/published?pageNumber=1&pageSize=2");
        filteredResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var filtered = await ReadJsonAsync(filteredResponse);
        filtered.GetProperty("totalCount").GetInt32().Should().Be(2);
        var items = filtered.GetProperty("items").EnumerateArray().ToList();
        items.Select(item => item.GetProperty("title").GetString()).Should().ContainInOrder("Available", "Low stock");
        items.Should().OnlyContain(item => item.GetProperty("isAvailable").GetBoolean());
        items.Single(item => item.GetProperty("title").GetString() == "Low stock")
            .GetProperty("isLowStock").GetBoolean().Should().BeTrue();
        items.Single(item => item.GetProperty("title").GetString() == "Low stock")
            .GetProperty("lowestAvailableStock").GetInt32().Should().Be(2);
        items.Single(item => item.GetProperty("title").GetString() == "Available")
            .GetProperty("compareAtPrice").GetDecimal().Should().Be(120m);

        using var enableAll = await client.PutAsJsonAsync("/api/store-settings/storefront", new
        {
            status = 2,
            statusMessage = "Kapalı",
            showOutOfStockProducts = true,
            showProductsWithoutPrice = true,
            defaultProductSort = 3,
            defaultProductSortDescending = false,
            showCompareAtPrice = true,
            showStockWarning = false,
            lowStockThreshold = 3,
            expectedConcurrencyToken = updatedToken
        });
        enableAll.StatusCode.Should().Be(HttpStatusCode.OK);

        using var explicitResponse = await client.GetAsync(
            "/api/products/published?pageNumber=1&pageSize=10&sortBy=Title&descending=true");
        var explicitResult = await ReadJsonAsync(explicitResponse);
        explicitResult.GetProperty("totalCount").GetInt32().Should().Be(4);
        explicitResult.GetProperty("items").EnumerateArray().First()
            .GetProperty("title").GetString().Should().Be("Out of stock");
        explicitResult.GetProperty("items").EnumerateArray()
            .Should().OnlyContain(item => !item.GetProperty("isLowStock").GetBoolean());

        using var publicResponse = await client.GetAsync("/api/store-settings");
        var publicSettings = await ReadJsonAsync(publicResponse);
        publicSettings.GetProperty("status").GetInt32().Should().Be(2);
        publicSettings.GetProperty("statusMessage").GetString().Should().Be("Kapalı");
        using var adminResponse = await client.GetAsync("/api/store-settings/admin");
        adminResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // Burada StoreSettings scalar alt sorgularıyla count ve items için toplam iki katalog komutu çalıştığını doğruluyorum.
    [Fact]
    public async Task Published_Product_Query_Should_Not_Create_Per_Product_Queries()
    {
        await using var factory = new StoreSettingsApiFactory(TestIdentity.Admin);
        using var client = factory.CreateClient(CreateClientOptions());
        await factory.InitializeAsync();
        await factory.SeedProductsAsync();
        factory.ResetCommandCount();

        using var response = await client.GetAsync("/api/products/published?pageNumber=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(response)).GetProperty("items").GetArrayLength().Should().Be(4);
        factory.CommandCount.Should().Be(2);
    }

    // Burada OpenAPI'nin public security boş dizisini, typed DTO'ları ve numeric enum değerlerini ürettiğini doğruluyorum.
    [Fact]
    public async Task OpenApi_Should_Expose_Typed_StoreSettings_Contracts_And_Anonymous_Security()
    {
        await using var factory = new StoreSettingsApiFactory(TestIdentity.None);
        using var client = factory.CreateClient(CreateClientOptions());
        await factory.InitializeAsync();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        paths.GetProperty("/api/store-settings").GetProperty("get")
            .GetProperty("security").GetArrayLength().Should().Be(0);
        paths.GetProperty("/api/store-settings/admin").GetProperty("get").TryGetProperty("security", out _)
            .Should().BeFalse();
        foreach (var section in new[] { "identity", "contact", "legal", "seo", "storefront" })
        {
            paths.TryGetProperty($"/api/store-settings/{section}", out var path).Should().BeTrue();
            path.TryGetProperty("put", out _).Should().BeTrue();
        }

        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var statusSchema = schemas.GetProperty("StorefrontStatus");
        statusSchema.GetProperty("type").GetString().Should().Be("integer");
        statusSchema.GetProperty("enum").EnumerateArray().Select(value => value.GetInt32())
            .Should().Equal(0, 1, 2);
        schemas.GetProperty("PublicStoreSettingsDto").GetProperty("properties")
            .TryGetProperty("taxNumber", out _).Should().BeFalse();
    }

    // Burada kayıt silinmiş olsa bile public GET'in 200 vermesini ve admin GET'in singleton kaydı yeniden oluşturmasını doğruluyorum.
    [Fact]
    public async Task Missing_Record_Should_Not_Cause_500_And_Admin_Get_Should_Recreate_It()
    {
        await using var factory = new StoreSettingsApiFactory(TestIdentity.Admin);
        using var client = factory.CreateClient(CreateClientOptions());
        await factory.InitializeAsync();
        await factory.DeleteSettingsAsync();

        using var publicResponse = await client.GetAsync("/api/store-settings");
        publicResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(publicResponse)).GetProperty("displayName").GetString().Should().Be("Mağaza");
        using var adminResponse = await client.GetAsync("/api/store-settings/admin");
        adminResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await factory.CountSettingsAsync()).Should().Be(1);
    }

    // Burada admin ayar cevabını gerçek HTTP hattından JSON olarak okuyorum.
    private static async Task<JsonElement> GetAdminAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/store-settings/admin");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await ReadJsonAsync(response);
    }

    // Burada HTTP JSON gövdesini response yaşam döngüsünden bağımsız bir element olarak kopyalıyorum.
    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    // Burada Problem Details hata kodunu ortak sözleşmeye göre doğruluyorum.
    private static async Task AssertProblemCodeAsync(HttpResponseMessage response, string expectedCode)
    {
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        (await ReadJsonAsync(response)).GetProperty("code").GetString().Should().Be(expectedCode);
    }

    // Burada paralel ve validation testlerinde kullanılan kimlik isteğini oluşturuyorum.
    private static object CreateIdentityRequest(string displayName, Guid token, string? logoUrl = null) =>
        new
        {
            displayName,
            shortDescription = (string?)null,
            logoUrl,
            darkLogoUrl = (string?)null,
            faviconUrl = (string?)null,
            defaultShareImageUrl = (string?)null,
            expectedConcurrencyToken = token
        };

    // Burada test istemcisinin HTTPS taban adresi ve yönlendirme davranışını hazırlıyorum.
    private static WebApplicationFactoryClientOptions CreateClientOptions() =>
        new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        };

    private sealed class StoreSettingsApiFactory : WebApplicationFactory<Program>
    {
        private readonly TestIdentity _identity;
        private readonly CountingDbCommandInterceptor _commandCounter = new();
        private readonly string _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"ecommerce-store-settings-{Guid.NewGuid():N}.db");

        // Burada test hostunun anonim, müşteri veya admin kimliğiyle çalışmasını seçiyorum.
        public StoreSettingsApiFactory(TestIdentity identity)
        {
            _identity = identity;
        }

        // Burada gerçek API hattını izole ilişkisel SQLite veritabanına bağlıyorum.
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "unused-by-test-override");
            builder.UseSetting("Jwt:Issuer", "ECommerce.StoreSettingsTests");
            builder.UseSetting("Jwt:Audience", "ECommerce.StoreSettingsTests.Client");
            builder.UseSetting("Jwt:SecretKey", "store-settings-test-secret-key-at-least-32-bytes");
            builder.UseSetting(
                "DataProtection:KeyRingPath",
                Path.Combine(Path.GetTempPath(), "ECommerce.StoreSettingsTests.DataProtection"));
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
                    options
                        .UseSqlite($"Data Source={_databasePath};Foreign Keys=True;Default Timeout=30")
                        .AddInterceptors(_commandCounter));
                if (_identity != TestIdentity.None)
                {
                    services
                        .AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = StoreSettingsTestAuthenticationHandler.SchemeName;
                            options.DefaultChallengeScheme = StoreSettingsTestAuthenticationHandler.SchemeName;
                            options.DefaultForbidScheme = StoreSettingsTestAuthenticationHandler.SchemeName;
                        })
                        .AddScheme<StoreSettingsTestAuthenticationOptions, StoreSettingsTestAuthenticationHandler>(
                            StoreSettingsTestAuthenticationHandler.SchemeName,
                            options => options.Identity = _identity);
                }
            });
        }

        // Burada SQLite şemasını ve singleton seed kaydını oluşturuyorum.
        public async Task InitializeAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync();
        }

        // Burada storefront görünürlük ve sıralama testleri için dört farklı ürün durumu hazırlıyorum.
        public async Task SeedProductsAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var available = CreateProduct("Available", "available", "AVAILABLE", 5, 100m, 120m);
            var lowStock = CreateProduct("Low stock", "low-stock", "LOW", 2, 80m);
            var outOfStock = CreateProduct("Out of stock", "out-of-stock", "OUT", 0, 90m);
            var withoutPrice = new Product(
                "No price",
                "no-price",
                "NO-PRICE-MAIN",
                status: ProductStatus.Active);
            context.Products.AddRange(available, lowStock, outOfStock, withoutPrice);
            await context.SaveChangesAsync();
        }

        // Burada ayar kaydı bulunmayan ilk çalışma senaryosunu oluşturuyorum.
        public async Task DeleteSettingsAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.StoreSettings.ExecuteDeleteAsync();
        }

        // Burada veritabanındaki singleton ayar kaydı sayısını okuyorum.
        public async Task<int> CountSettingsAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await context.StoreSettings.CountAsync();
        }

        // Burada global SEO güncellemesinden bağımsız kalması gereken ürün SEO alanlarını okuyorum.
        public async Task<(string? SeoTitle, string? SeoDescription)> ReadProductSeoAsync(string url)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await context.Products
                .Where(product => product.Url == url)
                .Select(product => new ValueTuple<string?, string?>(product.SeoTitle, product.SeoDescription))
                .SingleAsync();
        }

        public int CommandCount => _commandCounter.CommandCount;

        // Burada katalog isteği öncesinde SQL komut sayacını sıfırlıyorum.
        public void ResetCommandCount() =>
            _commandCounter.Reset();

        // Burada ürün görünürlük testi için stoklu veya stoksuz aktif varyantlı ürün oluşturuyorum.
        private static Product CreateProduct(
            string title,
            string url,
            string sku,
            int stock,
            decimal price,
            decimal? compareAtPrice = null)
        {
            var product = new Product(
                title,
                url,
                $"{sku}-MAIN",
                status: ProductStatus.Active,
                seoTitle: $"{title} SEO",
                seoDescription: $"{title} SEO description");
            product.Variants.Add(new ProductVariant(
                product,
                "Standard",
                $"{sku}-STD",
                price,
                stock,
                compareAtPrice));
            return product;
        }

        // Burada test hostunu kapattıktan sonra geçici SQLite dosyasını temizliyorum.
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && File.Exists(_databasePath))
            {
                SqliteConnection.ClearAllPools();
                File.Delete(_databasePath);
            }
        }
    }

    private sealed class CountingDbCommandInterceptor : DbCommandInterceptor
    {
        private int _commandCount;

        public int CommandCount => Volatile.Read(ref _commandCount);

        // Burada yeni ölçüm öncesinde ilişkisel komut sayacını sıfırlıyorum.
        public void Reset() =>
            Interlocked.Exchange(ref _commandCount, 0);

        // Burada eşzamanlı reader komutlarını N+1 ölçümü için sayıyorum.
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Interlocked.Increment(ref _commandCount);
            return result;
        }

        // Burada asenkron reader komutlarını N+1 ölçümü için sayıyorum.
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _commandCount);
            return ValueTask.FromResult(result);
        }

        // Burada eşzamanlı scalar komutlarını N+1 ölçümü için sayıyorum.
        public override InterceptionResult<object> ScalarExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result)
        {
            Interlocked.Increment(ref _commandCount);
            return result;
        }

        // Burada asenkron scalar komutlarını N+1 ölçümü için sayıyorum.
        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _commandCount);
            return ValueTask.FromResult(result);
        }
    }

    private sealed class StoreSettingsTestAuthenticationOptions : AuthenticationSchemeOptions
    {
        public TestIdentity Identity { get; set; }
    }

    private sealed class StoreSettingsTestAuthenticationHandler
        : AuthenticationHandler<StoreSettingsTestAuthenticationOptions>
    {
        public const string SchemeName = "StoreSettingsTest";

        // Burada test kimliğini authentication handler altyapısına bağlıyorum.
        public StoreSettingsTestAuthenticationHandler(
            IOptionsMonitor<StoreSettingsTestAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        // Burada seçilen admin veya müşteri rolünü gerçek authorization politikasına taşıyorum.
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var role = Options.Identity == TestIdentity.Admin
                ? UserRole.Admin.ToString()
                : UserRole.Customer.ToString();
            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "U00001"),
                    new Claim(ClaimTypes.Role, role)
                ],
                SchemeName);
            var principal = new ClaimsPrincipal(identity);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(principal, SchemeName)));
        }
    }

    private enum TestIdentity
    {
        None,
        Customer,
        Admin
    }
}
