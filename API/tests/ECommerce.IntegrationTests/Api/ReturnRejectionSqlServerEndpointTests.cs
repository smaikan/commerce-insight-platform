using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.IntegrationTests.Persistence;
using ECommerce.Persistence.Context;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.IntegrationTests.Api;

public sealed class ReturnRejectionSqlServerEndpointTests
{
    // Burada Delivered bildirimi mevcutken Received iade reddinin HTTP hattında atomik kalıp ikinci Delivered outbox üretmediğini doğruluyorum.
    [SqlServerFact]
    public async Task Reject_Should_Persist_Rejection_Without_Duplicate_Delivered_Outbox()
    {
        var databaseName = $"ECommerceReturnRejectHttp_{Guid.NewGuid():N}";
        var factory = new ReturnRejectionApiFactory(databaseName);

        try
        {
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });
            var seeded = await SeedReceivedReturnAsync(factory.Services);

            using var response = await client.PostAsJsonAsync(
                $"/api/returns/{seeded.ReturnRequestId:D}/reject",
                new { decisionNote = "Ürün iade koşullarını karşılamıyor." });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            await AssertPersistedResultAsync(factory.Services, seeded);
        }
        finally
        {
            await factory.DisposeAsync();
            await DeleteDatabaseAsync(databaseName);
        }
    }

    // Burada gerçek SQL Server testi için teslim edilmiş sipariş, mevcut Delivered bildirimi ve Received refund talebini hazırlıyorum.
    private static async Task<SeededReturn> SeedReceivedReturnAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var lifecycleAt = new DateTime(2026, 8, 24, 10, 0, 0, DateTimeKind.Utc);
        var product = new Product(
            "Return rejection SQL product",
            $"return-rejection-{Guid.NewGuid():N}",
            $"RET-REJ-{Guid.NewGuid():N}"[..30]);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var variant = new ProductVariant(
            product.Id,
            "Default",
            $"RET-REJ-VAR-{Guid.NewGuid():N}"[..30],
            100m,
            3);
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        var order = new Order(null, $"ORD-{Guid.NewGuid():N}"[..24], 100m, 0m, 0m, 0m, 100m);
        order.SetCustomerSnapshot("Ada", "Lovelace", "return-rejection@example.test", "+905551112233");
        var orderItem = order.AddItem(product.Id, variant.Id, product.Title, variant.Sku, 100m, 1);
        order.EnsureItemsMatchSubTotal();
        order.ChangeStatus(OrderStatus.Confirmed, lifecycleAt);
        var payment = new Payment(order.Id, PaymentProvider.Fake, order.GrandTotal, $"return_reject_{Guid.NewGuid():N}");
        order.AddPayment(payment);
        payment.MarkAsPaid($"transaction_{Guid.NewGuid():N}");
        order.ChangeStatus(OrderStatus.Paid, lifecycleAt.AddMinutes(1));
        order.ChangeStatus(OrderStatus.Preparing, lifecycleAt.AddMinutes(2));
        order.ChangeStatus(OrderStatus.Shipped, lifecycleAt.AddMinutes(3));
        order.ChangeStatus(OrderStatus.Delivered, lifecycleAt.AddMinutes(4));
        context.Orders.Add(order);
        context.EmailOutbox.Add(EmailOutboxMessage.CreateOrderStatusChanged(
            order.CustomerSnapshot!.Email,
            "Ada Lovelace",
            order.Id,
            order.OrderNumber,
            OrderStatus.Delivered,
            lifecycleAt.AddMinutes(4)));
        await context.SaveChangesAsync();

        var returnRequest = new ReturnRequest(
            order.Id,
            order.UserId,
            $"RET-{Guid.NewGuid():N}"[..30],
            ReturnType.Refund);
        returnRequest.AddItem(orderItem, 1);
        order.MarkReturnRequested();
        returnRequest.Receive(lifecycleAt.AddMinutes(5));
        context.ReturnRequests.Add(returnRequest);
        await context.SaveChangesAsync();
        var initialStockMovementCount = await context.StockMovements
            .CountAsync(movement => movement.ProductVariantId == variant.Id);

        return new SeededReturn(order.Id, returnRequest.Id, variant.Id, initialStockMovementCount);
    }

    // Burada ret sonucunun durum, stok ve outbox değişmezlerini yeni DbContext üzerinden birlikte doğruluyorum.
    private static async Task AssertPersistedResultAsync(IServiceProvider services, SeededReturn seeded)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var returnRequest = await context.ReturnRequests
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == seeded.ReturnRequestId);
        var order = await context.Orders
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == seeded.OrderId);
        var stockMovementCount = await context.StockMovements
            .AsNoTracking()
            .CountAsync(movement => movement.ProductVariantId == seeded.VariantId);
        var outbox = await context.EmailOutbox
            .AsNoTracking()
            .Where(message => message.OrderNumber == order.OrderNumber)
            .ToListAsync();

        returnRequest.Status.Should().Be(ReturnRequestStatus.Rejected);
        order.Status.Should().Be(OrderStatus.Delivered);
        stockMovementCount.Should().Be(seeded.InitialStockMovementCount);
        outbox.Should().ContainSingle(message =>
            message.Type == EmailOutboxMessageType.ReturnStatusChanged &&
            message.Status == ReturnRequestStatus.Rejected.ToString());
        outbox.Should().ContainSingle(message =>
            message.Type == EmailOutboxMessageType.OrderStatusChanged &&
            message.Status == OrderStatus.Delivered.ToString());
        outbox.Should().HaveCount(2);
    }

    // Burada SQL Server test veritabanını assertion sonrasında bağlantılar kapalıyken temizliyorum.
    private static async Task DeleteDatabaseAsync(string databaseName)
    {
        await using var context = new AppDbContext(CreateOptions(databaseName));
        await context.Database.EnsureDeletedAsync();
    }

    // Burada Docker veya LocalDB ortamına göre izole SQL Server veritabanı seçeneklerini oluşturuyorum.
    private static DbContextOptions<AppDbContext> CreateOptions(string databaseName)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(CreateConnectionString(databaseName))
            .Options;
    }

    // Burada test hostu ile cleanup işleminin paylaşacağı güvenli SQL Server bağlantısını oluşturuyorum.
    private static string CreateConnectionString(string databaseName)
    {
        var server = Environment.GetEnvironmentVariable("ECOMMERCE_TEST_SQL_SERVER");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
        return OperatingSystem.IsWindows() &&
               (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(password))
            ? $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;"
            : new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = databaseName,
                UserID = "sa",
                Password = password,
                TrustServerCertificate = true,
                MultipleActiveResultSets = true
            }.ConnectionString;
    }

    private sealed record SeededReturn(
        Guid OrderId,
        Guid ReturnRequestId,
        Guid VariantId,
        int InitialStockMovementCount);

    private sealed class ReturnRejectionApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName;

        // Burada gerçek API hostunu izole SQL Server veritabanına bağlayacak test fabrikasını hazırlıyorum.
        public ReturnRejectionApiFactory(string databaseName)
        {
            _databaseName = databaseName;
        }

        // Burada arka plan worker'larını kapatıp gerçek controller, MediatR, transaction ve persistence hattını çalıştırıyorum.
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:DefaultConnection", CreateConnectionString(_databaseName));
            builder.UseSetting("ENABLE_DEVELOPMENT_SEED", "false");
            builder.UseSetting("Jwt:Issuer", "ECommerce.ReturnRejectionTests");
            builder.UseSetting("Jwt:Audience", "ECommerce.ReturnRejectionTests.Client");
            builder.UseSetting("Jwt:SecretKey", "return-rejection-test-secret-key-at-least-32-bytes");
            builder.UseSetting("Email:PasswordResetUrl", "https://store.test/reset-password");
            builder.UseSetting("Email:Smtp:Password", "return-rejection-test-smtp-secret");
            builder.UseSetting("ContactPrivacy:NoticeVersion", "return-rejection-test-v1");
            builder.UseSetting("ContactPrivacy:RetentionDays", "60");
            builder.UseSetting(
                "DataProtection:KeyRingPath",
                Path.Combine(Path.GetTempPath(), $"ECommerce.ReturnRejectionTests.{_databaseName}"));
            builder.ConfigureServices(services =>
            {
                foreach (var hostedService in services
                             .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
                             .ToList())
                {
                    services.Remove(hostedService);
                }
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = AdminAuthenticationHandler.SchemeName;
                        options.DefaultChallengeScheme = AdminAuthenticationHandler.SchemeName;
                        options.DefaultForbidScheme = AdminAuthenticationHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, AdminAuthenticationHandler>(
                        AdminAuthenticationHandler.SchemeName,
                        _ => { });
            });
        }
    }

    private sealed class AdminAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "ReturnRejectionAdminTest";

        // Burada test authentication handler bağımlılıklarını framework tabanına iletiyorum.
        public AdminAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        // Burada reject endpointini gerçek AdminOnly politikası üzerinden yetkili test yöneticisiyle çağırıyorum.
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "U00001"),
                    new Claim(ClaimTypes.Role, UserRole.Admin.ToString())
                ],
                SchemeName);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
