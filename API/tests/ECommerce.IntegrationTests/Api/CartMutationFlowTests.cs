using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ECommerce.Application.Carts.Dtos;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Dtos;
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

public sealed class CartMutationFlowTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Burada kalıcı sepeti olmayan misafirin ilk ürünü ekleyebildiğini doğruluyorum.
    [Fact]
    public async Task Empty_Guest_Cart_Should_Accept_First_Item()
    {
        await using var scenario = await CartScenario.CreateAsync();

        var emptyCart = await GetCartAsync(scenario.Client);
        var added = await PostItemAsync(scenario.Client, scenario.FirstVariantId, 1, null);

        emptyCart.StatusCode.Should().Be(HttpStatusCode.OK);
        emptyCart.Cart!.Id.Should().BeNull();
        added.StatusCode.Should().Be(HttpStatusCode.OK);
        added.Cart!.Items.Should().ContainSingle();
        added.Cart.ConcurrencyToken.Should().NotBeNull();
    }

    // Burada mevcut sepete güncel token ile farklı ikinci varyantın eklenebildiğini doğruluyorum.
    [Fact]
    public async Task Existing_Cart_Should_Accept_Different_Second_Variant()
    {
        await using var scenario = await CartScenario.CreateAsync();
        var first = await PostItemAsync(scenario.Client, scenario.FirstVariantId, 1, null);
        var current = await GetCartAsync(scenario.Client);

        var second = await PostItemAsync(
            scenario.Client,
            scenario.SecondVariantId,
            1,
            current.Cart!.ConcurrencyToken);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        current.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        second.Cart!.Items.Should().HaveCount(2);
        second.Cart.Items.Select(item => item.ProductVariantId)
            .Should().BeEquivalentTo([scenario.FirstVariantId, scenario.SecondVariantId]);
    }

    // Burada ilk satırın adedi artırıldıktan sonra farklı varyant ekleme akışının çalıştığını doğruluyorum.
    [Fact]
    public async Task Quantity_Increase_Then_Different_Item_Add_Should_Succeed()
    {
        await using var scenario = await CartScenario.CreateAsync();
        var first = await PostItemAsync(scenario.Client, scenario.FirstVariantId, 1, null);
        var firstItem = first.Cart!.Items.Single();
        var increased = await PutQuantityAsync(
            scenario.Client,
            firstItem.Id,
            2,
            first.Cart.ConcurrencyToken!.Value);

        var second = await PostItemAsync(
            scenario.Client,
            scenario.SecondVariantId,
            1,
            increased.Cart!.ConcurrencyToken);

        increased.StatusCode.Should().Be(HttpStatusCode.OK);
        increased.Cart.Items.Single().Quantity.Should().Be(2);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        second.Cart!.Items.Should().HaveCount(2);
    }

    // Burada kaydı korunan boş sepete son token ile yeniden ürün eklenebildiğini doğruluyorum.
    [Fact]
    public async Task Cleared_Cart_Should_Accept_A_New_Item()
    {
        await using var scenario = await CartScenario.CreateAsync();
        var first = await PostItemAsync(scenario.Client, scenario.FirstVariantId, 1, null);
        var cleared = await ClearCartAsync(
            scenario.Client,
            first.Cart!.ConcurrencyToken!.Value);
        var current = await GetCartAsync(scenario.Client);

        var addedAgain = await PostItemAsync(
            scenario.Client,
            scenario.SecondVariantId,
            1,
            current.Cart!.ConcurrencyToken);

        cleared.StatusCode.Should().Be(HttpStatusCode.OK);
        cleared.Cart!.Id.Should().Be(first.Cart.Id);
        cleared.Cart.Items.Should().BeEmpty();
        current.StatusCode.Should().Be(HttpStatusCode.OK);
        addedAgain.StatusCode.Should().Be(HttpStatusCode.OK);
        addedAgain.Cart!.Items.Should().ContainSingle();
    }

    // Burada eski token ile yapılan gerçek mutation'ın concurrency conflict olarak reddedildiğini doğruluyorum.
    [Fact]
    public async Task Stale_Token_Should_Return_Concurrency_Conflict()
    {
        await using var scenario = await CartScenario.CreateAsync();
        var first = await PostItemAsync(scenario.Client, scenario.FirstVariantId, 1, null);
        var staleToken = first.Cart!.ConcurrencyToken!.Value;
        var second = await PostItemAsync(
            scenario.Client,
            scenario.SecondVariantId,
            1,
            staleToken);

        var conflict = await PutQuantityAsync(
            scenario.Client,
            first.Cart.Items.Single().Id,
            2,
            staleToken);

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        second.Cart!.ConcurrencyToken.Should().NotBe(staleToken);
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        conflict.ErrorCode.Should().Be("concurrency_conflict");
    }

    // Burada her başarılı sepet mutation'ının istemciye yeni bir concurrency tokenı döndürdüğünü doğruluyorum.
    [Fact]
    public async Task Every_Successful_Mutation_Should_Return_A_New_Concurrency_Token()
    {
        await using var scenario = await CartScenario.CreateAsync();
        var first = await PostItemAsync(scenario.Client, scenario.FirstVariantId, 1, null);
        var increased = await PutQuantityAsync(
            scenario.Client,
            first.Cart!.Items.Single().Id,
            2,
            first.Cart.ConcurrencyToken!.Value);
        var second = await PostItemAsync(
            scenario.Client,
            scenario.SecondVariantId,
            1,
            increased.Cart!.ConcurrencyToken);
        var cleared = await ClearCartAsync(
            scenario.Client,
            second.Cart!.ConcurrencyToken!.Value);
        var addedAgain = await PostItemAsync(
            scenario.Client,
            scenario.FirstVariantId,
            1,
            cleared.Cart!.ConcurrencyToken);

        var tokens = new[]
        {
            first.Cart.ConcurrencyToken,
            increased.Cart.ConcurrencyToken,
            second.Cart.ConcurrencyToken,
            cleared.Cart.ConcurrencyToken,
            addedAgain.Cart!.ConcurrencyToken
        };
        tokens.Should().OnlyContain(token => token.HasValue && token.Value != Guid.Empty);
        tokens.Should().OnlyHaveUniqueItems();
    }

    // Burada reddedilen stale-token mutation'ının sepeti veya etkileşim metriklerini değiştirmediğini doğruluyorum.
    [Fact]
    public async Task Failed_Mutation_Should_Not_Change_Cart_Or_Metrics()
    {
        await using var scenario = await CartScenario.CreateAsync();
        var first = await PostItemAsync(scenario.Client, scenario.FirstVariantId, 1, null);
        var staleToken = first.Cart!.ConcurrencyToken!.Value;
        var increased = await PutQuantityAsync(
            scenario.Client,
            first.Cart.Items.Single().Id,
            2,
            staleToken);
        var cartBeforeFailure = await GetCartAsync(scenario.Client);
        var metricsBeforeFailure = await scenario.ReadMetricsAsync();

        var conflict = await PostItemAsync(
            scenario.Client,
            scenario.SecondVariantId,
            1,
            staleToken);

        var cartAfterFailure = await GetCartAsync(scenario.Client);
        var metricsAfterFailure = await scenario.ReadMetricsAsync();
        increased.StatusCode.Should().Be(HttpStatusCode.OK);
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        conflict.ErrorCode.Should().Be("concurrency_conflict");
        cartAfterFailure.Cart.Should().BeEquivalentTo(cartBeforeFailure.Cart);
        metricsAfterFailure.Should().BeEquivalentTo(metricsBeforeFailure);
    }

    // Burada gerçek HTTP sepet cevaplarının varyantlı üründe ad-değeri koruyup varyantsız üründe teknik metni gizlediğini doğruluyorum.
    [Fact]
    public async Task Cart_Http_Responses_Should_Expose_Lossless_Variant_Selection()
    {
        await using var scenario = await CartScenario.CreateAsync();

        var added = await PostItemAsync(scenario.Client, scenario.FirstVariantId, 1, null);
        var read = await GetCartAsync(scenario.Client);
        var increased = await PutQuantityAsync(
            scenario.Client,
            read.Cart!.Items.Single().Id,
            2,
            read.Cart.ConcurrencyToken!.Value);
        var addedVariantless = await PostItemAsync(
            scenario.Client,
            scenario.SecondVariantId,
            1,
            increased.Cart!.ConcurrencyToken);

        added.StatusCode.Should().Be(HttpStatusCode.OK);
        added.Cart!.Items.Single().VariantName.Should().Be("Renk");
        added.Cart.Items.Single().VariantValue.Should().Be("Pudra");
        read.StatusCode.Should().Be(HttpStatusCode.OK);
        read.Cart.Items.Single().VariantName.Should().Be("Renk");
        read.Cart.Items.Single().VariantValue.Should().Be("Pudra");
        increased.StatusCode.Should().Be(HttpStatusCode.OK);
        increased.Cart.Items.Single().VariantName.Should().Be("Renk");
        increased.Cart.Items.Single().VariantValue.Should().Be("Pudra");
        addedVariantless.StatusCode.Should().Be(HttpStatusCode.OK);
        addedVariantless.Cart!.Items.Single(item => item.ProductVariantId == scenario.SecondVariantId)
            .VariantName.Should().BeNull();
        addedVariantless.Cart.Items.Single(item => item.ProductVariantId == scenario.SecondVariantId)
            .VariantValue.Should().BeNull();
    }

    // Burada guest checkout HTTP cevabının seçilen varyantı sipariş anındaki değişmez ad-değer snapshot'ıyla döndürdüğünü doğruluyorum.
    [Fact]
    public async Task Guest_Checkout_Should_Return_Variant_Name_And_Value_Snapshot()
    {
        await using var scenario = await CartScenario.CreateAsync();
        var added = await PostItemAsync(scenario.Client, scenario.FirstVariantId, 1, null);

        var checkout = await GuestCheckoutAsync(
            scenario.Client,
            scenario.ShippingMethodId,
            added.Cart!.ConcurrencyToken!.Value);

        checkout.StatusCode.Should().Be(HttpStatusCode.Created);
        checkout.Order.Should().NotBeNull();
        checkout.Order!.Items.Should().ContainSingle();
        checkout.Order.Items.Single().VariantName.Should().Be("Renk");
        checkout.Order.Items.Single().VariantValue.Should().Be("Pudra");
    }

    // Burada sepete ürün ekleyen HTTP isteğini gerçek JSON sözleşmesiyle gönderiyorum.
    private static Task<CartHttpResult> PostItemAsync(
        HttpClient client,
        Guid variantId,
        int quantity,
        Guid? expectedConcurrencyToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/cart/items")
        {
            Content = JsonContent.Create(new
            {
                productVariantId = variantId,
                quantity,
                expectedConcurrencyToken
            })
        };
        return SendCartRequestAsync(client, request);
    }

    // Burada sepet satırı adedini tokenla güncelleyen HTTP isteğini gönderiyorum.
    private static Task<CartHttpResult> PutQuantityAsync(
        HttpClient client,
        Guid cartItemId,
        int quantity,
        Guid expectedConcurrencyToken)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/api/cart/items/{cartItemId:D}")
        {
            Content = JsonContent.Create(new { quantity, expectedConcurrencyToken })
        };
        return SendCartRequestAsync(client, request);
    }

    // Burada sepeti güncel tokenla temizleyen HTTP isteğini gönderiyorum.
    private static Task<CartHttpResult> ClearCartAsync(
        HttpClient client,
        Guid expectedConcurrencyToken)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/cart?expectedConcurrencyToken={expectedConcurrencyToken:D}");
        return SendCartRequestAsync(client, request);
    }

    // Burada misafir sepetinin son halini gerçek GET endpointinden okuyorum.
    private static Task<CartHttpResult> GetCartAsync(HttpClient client)
    {
        return SendCartRequestAsync(
            client,
            new HttpRequestMessage(HttpMethod.Get, "/api/cart"));
    }

    // Burada gerçek guest checkout endpointine gerekli origin ve idempotency başlıklarıyla tipli istek gönderiyorum.
    private static async Task<OrderHttpResult> GuestCheckoutAsync(
        HttpClient client,
        Guid shippingMethodId,
        Guid expectedCartConcurrencyToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/cart/checkout/guest")
        {
            Content = JsonContent.Create(new
            {
                expectedCartConcurrencyToken,
                customer = new
                {
                    firstName = "Ada",
                    lastName = "Yılmaz",
                    email = "guest-variant@example.com",
                    phoneNumber = "05000000000"
                },
                shippingAddress = new
                {
                    title = "Ev",
                    firstName = "Ada",
                    lastName = "Yılmaz",
                    phoneNumber = "05000000000",
                    city = "İzmir",
                    district = "Konak",
                    fullAddress = "Test Sokak 1",
                    postalCode = "35220"
                },
                billingAddress = (object?)null,
                shippingMethodId
            })
        };
        request.Headers.TryAddWithoutValidation("Origin", "https://localhost");
        request.Headers.TryAddWithoutValidation("Idempotency-Key", $"variant-checkout-{Guid.NewGuid():N}");
        request.Headers.TryAddWithoutValidation("X-Turnstile-Token", "valid-test-token");
        using var response = await client.SendAsync(request);
        var order = response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<OrderDto>(JsonOptions)
            : null;
        return new OrderHttpResult(response.StatusCode, order);
    }

    // Burada başarılı sepet gövdesini veya Problem Details hata kodunu ortak biçimde okuyorum.
    private static async Task<CartHttpResult> SendCartRequestAsync(
        HttpClient client,
        HttpRequestMessage request)
    {
        using (request)
        using (var response = await client.SendAsync(request))
        {
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var cart = JsonSerializer.Deserialize<CartDto>(content, JsonOptions);
                cart.Should().NotBeNull();
                return new CartHttpResult(response.StatusCode, cart, null);
            }

            using var problem = JsonDocument.Parse(content);
            var errorCode = problem.RootElement.TryGetProperty("code", out var code)
                ? code.GetString()
                : null;
            return new CartHttpResult(response.StatusCode, null, errorCode);
        }
    }

    private sealed class CartScenario : IAsyncDisposable
    {
        private readonly CartApiFactory _factory;

        public HttpClient Client { get; }
        public Guid FirstVariantId { get; }
        public Guid SecondVariantId { get; }
        public Guid ShippingMethodId { get; }

        // Burada her testin izole API hostunu, clientını ve katalog kimliklerini saklıyorum.
        private CartScenario(
            CartApiFactory factory,
            HttpClient client,
            Guid firstVariantId,
            Guid secondVariantId,
            Guid shippingMethodId)
        {
            _factory = factory;
            Client = client;
            FirstVariantId = firstVariantId;
            SecondVariantId = secondVariantId;
            ShippingMethodId = shippingMethodId;
        }

        // Burada ilişkisel test veritabanını hazırlayıp iki satılabilir varyantlı yeni HTTP senaryosu kuruyorum.
        public static async Task<CartScenario> CreateAsync()
        {
            var factory = new CartApiFactory();
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "Cookie",
                $"ecommerce_guest_cart={CreateGuestSessionId()}");
            var variantIds = await factory.InitializeAndSeedAsync();
            return new CartScenario(
                factory,
                client,
                variantIds.First,
                variantIds.Second,
                variantIds.ShippingMethodId);
        }

        // Burada başarısız mutation öncesi ve sonrası karşılaştırılacak kalıcı metrik durumunu okuyorum.
        public Task<CartMetricSnapshot> ReadMetricsAsync()
        {
            return _factory.ReadMetricsAsync();
        }

        // Burada test senaryosunun HTTP clientını ve uygulama hostunu kapatıyorum.
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _factory.DisposeAsync();
        }

        // Burada test cookie'si için loglanmayan 256 bitlik kanonik misafir oturumu üretiyorum.
        private static string CreateGuestSessionId()
        {
            return Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        }
    }

    private sealed class CartApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:;Foreign Keys=True");

        // Burada test hostunun kullandığı tek ilişkisel SQLite bağlantısını açık tutuyorum.
        public CartApiFactory()
        {
            _connection.Open();
        }

        // Burada production SQL Server kaydını izole SQLite bağlantısıyla değiştirip arka plan işlerini kapatıyorum.
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "unused-by-test-override");
            builder.UseSetting("Jwt:Issuer", "ECommerce.CartIntegrationTests");
            builder.UseSetting("Jwt:Audience", "ECommerce.CartIntegrationTests.Client");
            builder.UseSetting("Jwt:SecretKey", "cart-integration-test-secret-key-at-least-32-bytes");
            builder.UseSetting("GuestProtection:TrustedOrigins", "https://localhost");
            builder.UseSetting(
                "DataProtection:KeyRingPath",
                Path.Combine(Path.GetTempPath(), "ECommerce.CartIntegrationTests.DataProtection"));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.RemoveAll<ITurnstileVerifier>();
                foreach (var hostedService in services
                    .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
                    .ToList())
                {
                    services.Remove(hostedService);
                }

                services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
                services.AddSingleton<ITurnstileVerifier, ValidTurnstileVerifier>();
            });
        }

        // Burada şemayı oluşturup iki ayrı aktif ürün ve varyantı test veritabanına ekliyorum.
        public async Task<(Guid First, Guid Second, Guid ShippingMethodId)> InitializeAndSeedAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.EnsureCreatedAsync();
            var firstProduct = new Product(
                "Cart product one",
                "cart-product-one",
                "CART-PRODUCT-ONE",
                status: ProductStatus.Active,
                hasVariants: true);
            var secondProduct = new Product(
                "Cart product two",
                "cart-product-two",
                "CART-PRODUCT-TWO",
                status: ProductStatus.Active);
            context.Products.AddRange(firstProduct, secondProduct);
            await context.SaveChangesAsync();

            var firstVariant = new ProductVariant(
                firstProduct.Id,
                "Renk",
                "CART-VARIANT-ONE",
                100m,
                20,
                value: "Pudra");
            var secondVariant = new ProductVariant(
                secondProduct.Id,
                "Default",
                "CART-VARIANT-TWO",
                200m,
                20,
                value: "Default");
            context.ProductVariants.AddRange(firstVariant, secondVariant);
            var shippingMethod = new ShippingMethod("Standart", 0m);
            context.ShippingMethods.Add(shippingMethod);
            await context.SaveChangesAsync();
            return (firstVariant.Id, secondVariant.Id, shippingMethod.Id);
        }

        // Burada ürün, varyant ve günlük sayaçları deterministik satırlara dönüştürerek okuyorum.
        public async Task<CartMetricSnapshot> ReadMetricsAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var products = await context.Products.AsNoTracking()
                .OrderBy(product => product.Id)
                .Select(product => $"{product.Id}:{product.TotalAddToCartCount}:{product.PopularityScore}:{product.ConcurrencyToken}")
                .ToListAsync();
            var variants = await context.ProductVariants.AsNoTracking()
                .OrderBy(variant => variant.Id)
                .Select(variant => $"{variant.Id}:{variant.AddToCartCount}:{variant.ConcurrencyToken}")
                .ToListAsync();
            var productDailyMetrics = await context.ProductDailyMetrics.AsNoTracking()
                .OrderBy(metric => metric.ProductId)
                .ThenBy(metric => metric.Date)
                .Select(metric => $"{metric.ProductId}:{metric.Date}:{metric.AddToCartCount}")
                .ToListAsync();
            var variantDailyMetrics = await context.ProductVariantDailyMetrics.AsNoTracking()
                .OrderBy(metric => metric.ProductVariantId)
                .ThenBy(metric => metric.Date)
                .Select(metric => $"{metric.ProductVariantId}:{metric.Date}:{metric.AddToCartCount}")
                .ToListAsync();
            return new CartMetricSnapshot(
                products,
                variants,
                productDailyMetrics,
                variantDailyMetrics);
        }

        // Burada test hostuyla birlikte açık tutulan SQLite bağlantısını kapatıyorum.
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection.Dispose();
            }
        }
    }

    private sealed record CartHttpResult(
        HttpStatusCode StatusCode,
        CartDto? Cart,
        string? ErrorCode);

    private sealed record OrderHttpResult(HttpStatusCode StatusCode, OrderDto? Order);

    private sealed record CartMetricSnapshot(
        IReadOnlyList<string> Products,
        IReadOnlyList<string> Variants,
        IReadOnlyList<string> ProductDailyMetrics,
        IReadOnlyList<string> VariantDailyMetrics);

    private sealed class ValidTurnstileVerifier : ITurnstileVerifier
    {
        // Burada HTTP checkout testinin dış ağa çıkmadan geçerli Turnstile sonucu almasını sağlıyorum.
        public Task<TurnstileVerificationResult> VerifyAsync(
            string token,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(TurnstileVerificationResult.Valid);
        }
    }
}
