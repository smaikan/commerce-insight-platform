using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ECommerce.API.Controllers.Accounting;
using ECommerce.API.Security;
using ECommerce.Application.Accounting.SalesOrders;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.IntegrationTests.Api;

public sealed class AccountingSalesControllersTests
{
    // Burada Accounting satış controllerlarının beklenen route ve HTTP fiillerini eksiksiz yayımladığını doğruluyorum.
    [Fact]
    public void Controllers_Should_Expose_Expected_Endpoint_Contracts()
    {
        var actualContracts = GetEndpointContracts<AccountingSalesOrdersController>()
            .Concat(GetEndpointContracts<SalesInvoicesController>())
            .OrderBy(contract => contract)
            .ToArray();
        var expectedContracts = new[]
        {
            "DELETE /api/accounting/sales-invoices/{id:guid}/lines/{lineId:guid}",
            "DELETE /api/accounting/sales-orders/{id:guid}/items/{itemId:guid}",
            "GET /api/accounting/sales-invoices",
            "GET /api/accounting/sales-invoices/{id:guid}",
            "GET /api/accounting/sales-orders",
            "GET /api/accounting/sales-orders/{id:guid}",
            "POST /api/accounting/sales-invoices",
            "POST /api/accounting/sales-invoices/from-order/{accountingSalesOrderId:guid}",
            "POST /api/accounting/sales-invoices/{id:guid}/lines",
            "POST /api/accounting/sales-invoices/{id:guid}/cancel",
            "POST /api/accounting/sales-invoices/{id:guid}/post",
            "POST /api/accounting/sales-orders",
            "POST /api/accounting/sales-orders/{id:guid}/items",
            "POST /api/accounting/sales-orders/{id:guid}/cancel",
            "POST /api/accounting/sales-orders/{id:guid}/post",
            "PUT /api/accounting/sales-invoices/{id:guid}",
            "PUT /api/accounting/sales-invoices/{id:guid}/lines/{lineId:guid}",
            "PUT /api/accounting/sales-orders/{id:guid}",
            "PUT /api/accounting/sales-orders/{id:guid}/items/{itemId:guid}"
        };

        actualContracts.Should().Equal(expectedContracts.OrderBy(contract => contract));
    }

    // Burada her iki Accounting satış controllerının API ve AdminOnly yetkilendirme sınırını taşıdığını doğruluyorum.
    [Fact]
    public void Controllers_Should_Require_AdminOnly_Policy()
    {
        AssertControllerMetadata<AccountingSalesOrdersController>(
            "api/accounting/sales-orders");
        AssertControllerMetadata<SalesInvoicesController>(
            "api/accounting/sales-invoices");
    }

    // Burada tekrar güvenliği gereken oluşturma uçlarının idempotency anahtarını yalnız HTTP headerından aldığını doğruluyorum.
    [Fact]
    public void Creation_Endpoints_Should_Bind_Idempotency_Key_From_Header()
    {
        AssertIdempotencyHeader<AccountingSalesOrdersController>(
            nameof(AccountingSalesOrdersController.Create));
        AssertIdempotencyHeader<SalesInvoicesController>(
            nameof(SalesInvoicesController.CreateDirect));
    }

    // Burada Accounting satış HTTP girdilerinin e-ticaret kullanıcı, sepet, adres veya depo kimliği yayımlamadığını doğruluyorum.
    [Fact]
    public void Request_Contracts_Should_Not_Expose_Forbidden_Core_Identifiers()
    {
        var requestTypes = new[]
        {
            typeof(CreateAccountingSalesOrderRequest),
            typeof(UpdateAccountingSalesOrderRequest),
            typeof(AccountingSalesOrderItemRequest),
            typeof(AccountingSalesOrderItemUpdateRequest),
            typeof(CreateDirectSalesInvoiceRequest),
            typeof(CreateSalesInvoiceFromOrderRequest),
            typeof(UpdateSalesInvoiceRequest),
            typeof(AddSalesInvoiceLineRequest),
            typeof(UpdateSalesInvoiceLineRequest),
            typeof(AccountingSalesOrderHeaderInput),
            typeof(AccountingSalesOrderLineInput),
            typeof(SalesInvoiceLineUpdateInput),
            typeof(SalesInvoiceHeaderInput)
        };
        var forbiddenProperties = new HashSet<string>(
            ["UserId", "CartId", "AddressId", "WarehouseId"],
            StringComparer.Ordinal);

        requestTypes
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .Select(property => property.Name)
            .Should()
            .NotContain(name => forbiddenProperties.Contains(name));
    }

    // Burada tekil sipariş item güncelleme endpointinin ProductVariant veya kod alanı taşımayan ticari payload kullandığını doğruluyorum.
    [Fact]
    public void Order_Item_Update_Should_Not_Expose_Product_Identity()
    {
        var updateAction = typeof(AccountingSalesOrdersController)
            .GetMethod(nameof(AccountingSalesOrdersController.UpdateItem))
            ?? throw new InvalidOperationException("Sales order item update action was not found.");
        var requestType = updateAction.GetParameters()
            .Single(parameter => parameter.Name == "request")
            .ParameterType;
        var lineType = requestType.GetProperty(nameof(AccountingSalesOrderItemUpdateRequest.Line))
            ?.PropertyType;
        var forbiddenProperties = new[]
        {
            "ProductId",
            "ProductVariantId",
            "ProductName",
            "VariantName",
            "Sku",
            "Barcode",
            "LineNumber"
        };

        requestType.Should().Be(typeof(AccountingSalesOrderItemUpdateRequest));
        lineType.Should().Be(typeof(SalesInvoiceLineUpdateInput));
        lineType!.GetProperties()
            .Select(property => property.Name)
            .Should()
            .NotIntersectWith(forbiddenProperties);
    }

    // Burada gerçek HTTP pipelineının tüm Accounting satış uçlarında anonim çağrıyı handlera geçmeden 401 ile durdurduğunu doğruluyorum.
    [Fact]
    public async Task Accounting_Sales_Endpoints_Should_Reject_Anonymous_Http_Requests()
    {
        var orderId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var endpoints = new (HttpMethod Method, string Path)[]
        {
            (HttpMethod.Post, "/api/accounting/sales-orders"),
            (HttpMethod.Put, $"/api/accounting/sales-orders/{orderId}"),
            (HttpMethod.Post, $"/api/accounting/sales-orders/{orderId}/items"),
            (HttpMethod.Put, $"/api/accounting/sales-orders/{orderId}/items/{itemId}"),
            (HttpMethod.Delete, $"/api/accounting/sales-orders/{orderId}/items/{itemId}"),
            (HttpMethod.Post, $"/api/accounting/sales-orders/{orderId}/post"),
            (HttpMethod.Get, $"/api/accounting/sales-orders/{orderId}"),
            (HttpMethod.Get, "/api/accounting/sales-orders"),
            (HttpMethod.Post, "/api/accounting/sales-invoices"),
            (HttpMethod.Post, $"/api/accounting/sales-invoices/from-order/{orderId}"),
            (HttpMethod.Put, $"/api/accounting/sales-invoices/{orderId}"),
            (HttpMethod.Post, $"/api/accounting/sales-invoices/{orderId}/lines"),
            (HttpMethod.Put, $"/api/accounting/sales-invoices/{orderId}/lines/{itemId}"),
            (HttpMethod.Delete, $"/api/accounting/sales-invoices/{orderId}/lines/{itemId}"),
            (HttpMethod.Post, $"/api/accounting/sales-invoices/{orderId}/post"),
            (HttpMethod.Get, $"/api/accounting/sales-invoices/{orderId}"),
            (HttpMethod.Get, "/api/accounting/sales-invoices")
        };
        await using var factory = new AccountingSalesApiFactory();
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        foreach (var endpoint in endpoints)
        {
            using var request = new HttpRequestMessage(endpoint.Method, endpoint.Path);
            if (endpoint.Method == HttpMethod.Post || endpoint.Method == HttpMethod.Put)
            {
                request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            }

            using var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(
                HttpStatusCode.Unauthorized,
                $"{endpoint.Method} {endpoint.Path} AdminOnly politikasıyla korunmalıdır");
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
            using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            problem.RootElement.GetProperty("code").GetString().Should().Be("authentication_required");
            problem.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
        }
    }

    // Burada kimliği doğrulanmış fakat Admin rolü olmayan çağrının gerçek politika değerlendirmesinde 403 aldığını doğruluyorum.
    [Fact]
    public async Task Accounting_Sales_Endpoints_Should_Reject_Non_Admin_User()
    {
        await using var factory = new AccountingSalesApiFactory(authenticateAsNonAdmin: true);
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/api/accounting/sales-orders");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().Should().Be("forbidden");
        problem.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    // Burada controller route metadata bilgisini karşılaştırılabilir HTTP sözleşmelerine dönüştürüyorum.
    private static IReadOnlyList<string> GetEndpointContracts<TController>()
    {
        var controllerType = typeof(TController);
        var controllerRoute = controllerType.GetCustomAttribute<RouteAttribute>()?.Template
            ?? throw new InvalidOperationException($"{controllerType.Name} route metadata içermiyor.");
        var contracts = new List<string>();

        foreach (var method in controllerType.GetMethods(
                     BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            foreach (var httpAttribute in method.GetCustomAttributes<HttpMethodAttribute>(inherit: true))
            {
                var route = string.IsNullOrWhiteSpace(httpAttribute.Template)
                    ? controllerRoute
                    : $"{controllerRoute}/{httpAttribute.Template}";
                contracts.AddRange(
                    httpAttribute.HttpMethods.Select(httpMethod => $"{httpMethod} /{route}"));
            }
        }

        return contracts;
    }

    // Burada controllerın ApiController, sabit route ve tek AdminOnly politika metadata şartlarını birlikte kontrol ediyorum.
    private static void AssertControllerMetadata<TController>(string expectedRoute)
    {
        var controllerType = typeof(TController);

        controllerType.GetCustomAttribute<ApiControllerAttribute>(inherit: true)
            .Should()
            .NotBeNull();
        controllerType.GetCustomAttribute<RouteAttribute>(inherit: true)?.Template
            .Should()
            .Be(expectedRoute);
        controllerType.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Should()
            .ContainSingle()
            .Which.Policy.Should()
            .Be(AuthorizationPolicies.AdminOnly);
    }

    // Burada oluşturma actionındaki idempotency parametresinin adlandırılmış header binding metadata bilgisini kontrol ediyorum.
    private static void AssertIdempotencyHeader<TController>(string actionName)
    {
        var action = typeof(TController).GetMethod(actionName)
            ?? throw new InvalidOperationException($"{typeof(TController).Name}.{actionName} bulunamadı.");
        var headerParameter = action.GetParameters()
            .Single(parameter => parameter.Name == "idempotencyKey");

        headerParameter.GetCustomAttribute<FromHeaderAttribute>()?.Name
            .Should()
            .Be("Idempotency-Key");
    }

    private sealed class AccountingSalesApiFactory : WebApplicationFactory<Program>
    {
        private readonly bool _authenticateAsNonAdmin;

        // Burada test sunucusunun anonim veya Admin olmayan kimlikle çalışmasını seçiyorum.
        public AccountingSalesApiFactory(bool authenticateAsNonAdmin = false)
        {
            _authenticateAsNonAdmin = authenticateAsNonAdmin;
        }

        // Burada Accounting satış HTTP sözleşmesi testi için izole API sunucusu ayarlarını hazırlıyorum.
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Server=(localdb)\\mssqllocaldb;Database=ECommerceAccountingSalesHttpTests;Trusted_Connection=True;");
            builder.UseSetting("Jwt:Issuer", "ECommerce.IntegrationTests");
            builder.UseSetting("Jwt:Audience", "ECommerce.IntegrationTests.Client");
            builder.UseSetting(
                "Jwt:SecretKey",
                "integration-test-secret-key-at-least-32-bytes");
            builder.UseSetting(
                "DataProtection:KeyRingPath",
                Path.Combine(
                    Path.GetTempPath(),
                    "ECommerce.IntegrationTests.AccountingSales.DataProtection"));

            if (_authenticateAsNonAdmin)
            {
                // Burada gerçek AdminOnly değerlendirmesi için rol taşımayan test şemasını varsayılan yapıyorum.
                builder.ConfigureTestServices(services =>
                {
                    // Burada test kimliğinin authenticate, challenge ve forbid işlemlerini aynı şemaya bağlıyorum.
                    services
                        .AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                            options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                            options.DefaultForbidScheme = TestAuthenticationHandler.SchemeName;
                        })
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                            TestAuthenticationHandler.SchemeName,
                            _ => { });
                });
            }
        }
    }

    private sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "AccountingSalesNonAdminTest";

        // Burada gerçek authorization politikasına Admin rolü taşımayan doğrulanmış test kimliğini bağlıyorum.
        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        // Burada test isteğini Admin rolü içermeyen doğrulanmış bir kullanıcı olarak kabul ediyorum.
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "U00001")],
                SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
