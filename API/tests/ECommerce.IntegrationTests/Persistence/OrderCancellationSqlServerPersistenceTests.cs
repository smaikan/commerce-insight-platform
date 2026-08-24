using ECommerce.Application.Common.Exceptions;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class OrderCancellationSqlServerPersistenceTests
{
    // Burada SQL Server filtered unique indeksinin aynı sipariş için ikinci aktif finansal ters işlem niyetini reddettiğini doğruluyorum.
    [SqlServerFact]
    public async Task Database_Should_Reject_A_Second_Active_Cancellation_Operation()
    {
        var databaseName = $"ECommerceOrderCancellationUnique_{Guid.NewGuid():N}";
        var options = CreateOptions(databaseName);

        try
        {
            var seeded = await SeedPaidOrderAsync(options);
            await using var context = new AppDbContext(options);
            var order = await context.Orders
                .Include(candidate => candidate.Payments)
                .SingleAsync(candidate => candidate.Id == seeded.OrderId);
            var payment = order.Payments.Single(candidate => candidate.Id == seeded.PaymentId);
            context.OrderCancellationOperations.Add(new OrderCancellationOperation(
                order,
                payment,
                OrderCancellationInitiatorType.Member,
                PaymentReversalType.Cancel,
                DateTime.UtcNow));
            await context.SaveChangesAsync();
            context.OrderCancellationOperations.Add(new OrderCancellationOperation(
                order,
                payment,
                OrderCancellationInitiatorType.Guest,
                PaymentReversalType.Cancel,
                DateTime.UtcNow));

            var duplicateSave = () => context.SaveChangesAsync();

            await duplicateSave.Should().ThrowAsync<DbUpdateException>();
        }
        finally
        {
            await using var cleanupContext = new AppDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    // Burada cancellation concurrency tokenının gerçek SQL Server üzerinde stale worker yazımını reddettiğini doğruluyorum.
    [SqlServerFact]
    public async Task Database_Should_Reject_A_Stale_Cancellation_Lease_Update()
    {
        var databaseName = $"ECommerceOrderCancellationConcurrency_{Guid.NewGuid():N}";
        var options = CreateOptions(databaseName);

        try
        {
            var seeded = await SeedPaidOrderAsync(options);
            Guid operationId;
            await using (var seedContext = new AppDbContext(options))
            {
                var order = await seedContext.Orders
                    .Include(candidate => candidate.Payments)
                    .SingleAsync(candidate => candidate.Id == seeded.OrderId);
                var operation = new OrderCancellationOperation(
                    order,
                    order.Payments.Single(candidate => candidate.Id == seeded.PaymentId),
                    OrderCancellationInitiatorType.Member,
                    PaymentReversalType.Cancel,
                    DateTime.UtcNow);
                seedContext.OrderCancellationOperations.Add(operation);
                await seedContext.SaveChangesAsync();
                operationId = operation.Id;
            }

            await using var firstContext = new AppDbContext(options);
            await using var staleContext = new AppDbContext(options);
            var first = await firstContext.OrderCancellationOperations.SingleAsync(item => item.Id == operationId);
            var stale = await staleContext.OrderCancellationOperations.SingleAsync(item => item.Id == operationId);
            var claimAt = DateTime.UtcNow.AddMinutes(1);
            first.TryClaim(claimAt, TimeSpan.FromMinutes(2)).Should().BeTrue();
            stale.TryClaim(claimAt, TimeSpan.FromMinutes(2)).Should().BeTrue();
            await new UnitOfWork(firstContext).SaveChangesAsync();

            var staleSave = () => new UnitOfWork(staleContext).SaveChangesAsync();

            await staleSave.Should().ThrowAsync<ConcurrencyException>();
            firstContext.Model.FindEntityType(typeof(OrderCancellationOperation))!
                .FindProperty(nameof(OrderCancellationOperation.Id))!
                .ValueGenerated.Should().Be(ValueGenerated.Never);
            firstContext.Model.FindEntityType(typeof(OrderCancellationOperationItem))!
                .FindProperty(nameof(OrderCancellationOperationItem.Id))!
                .ValueGenerated.Should().Be(ValueGenerated.Never);
            firstContext.Model.FindEntityType(typeof(PaymentItemTransaction))!
                .FindProperty(nameof(PaymentItemTransaction.Id))!
                .ValueGenerated.Should().Be(ValueGenerated.Never);
        }
        finally
        {
            await using var cleanupContext = new AppDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    // Burada SQL Server testleri için tahsil edilmiş tek kalemli sipariş ve provider ödeme kimliğini kalıcılaştırıyorum.
    private static async Task<(Guid OrderId, Guid PaymentId)> SeedPaidOrderAsync(
        DbContextOptions<AppDbContext> options)
    {
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var product = new Product(
            "Cancellation SQL product",
            $"cancellation-sql-{Guid.NewGuid():N}",
            $"CAN-SQL-{Guid.NewGuid():N}"[..30]);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var variant = new ProductVariant(
            product.Id,
            "Default",
            $"CAN-SQL-VAR-{Guid.NewGuid():N}"[..30],
            10m,
            5);
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();
        var order = new Order(null, $"ORD-{Guid.NewGuid():N}"[..24], 10m, 0m, 0m, 0m, 10m);
        order.AddItem(product.Id, variant.Id, product.Title, variant.Sku, 10m, 1);
        order.EnsureItemsMatchSubTotal();
        order.ChangeStatus(OrderStatus.Confirmed, DateTime.UtcNow);
        var payment = new Payment(order.Id, PaymentProvider.Iyzico, 10m, $"sql_cancel_{Guid.NewGuid():N}");
        order.AddPayment(payment);
        payment.InitializeCheckoutForm(
            $"sql-token-{Guid.NewGuid():N}",
            payment.Id.ToString("N"),
            $"https://sandbox-cpp.iyzipay.com?token={Guid.NewGuid():N}",
            DateTime.UtcNow.AddMinutes(30));
        payment.MarkAsPaid($"provider-payment-{Guid.NewGuid():N}", 1, 10m, 1);
        order.ChangeStatus(OrderStatus.Paid, DateTime.UtcNow);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return (order.Id, payment.Id);
    }

    // Burada yalnız açıkça yapılandırılmış SQL Server test ortamına benzersiz veritabanı seçenekleri üretiyorum.
    private static DbContextOptions<AppDbContext> CreateOptions(string databaseName)
    {
        var server = Environment.GetEnvironmentVariable("ECOMMERCE_TEST_SQL_SERVER");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
        var connectionString = OperatingSystem.IsWindows() &&
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

        return new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;
    }
}
