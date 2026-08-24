using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Returns.Commands.ApproveReturnRequest;
using ECommerce.Application.Returns.Commands.ReceiveReturnRequest;
using ECommerce.Application.Returns.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using ECommerce.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.IntegrationTests.Persistence;

public sealed class ReturnLifecyclePersistenceTests
{
    // Burada yeni refund akışının teslimde stok değiştirmeyip onayda talep, sipariş ve stok hareketini birlikte kalıcılaştırdığını doğruluyorum.
    [Fact]
    public async Task Refund_Approval_Should_Persist_Status_And_Stock_After_Physical_Receipt()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        var seeded = await SeedRequestedRefundAsync(options);
        var receiveClock = new FixedClock(new DateTime(2026, 8, 23, 10, 0, 0, DateTimeKind.Utc));

        await using (var receiveContext = new AppDbContext(options))
        {
            var handler = new ReceiveReturnRequestCommandHandler(
                new ReturnRequestRepository(receiveContext),
                new OrderRepository(receiveContext),
                new ReturnInventoryService(new ProductVariantRepository(receiveContext)),
                receiveClock,
                new UnitOfWork(receiveContext));

            await handler.Handle(new ReceiveReturnRequestCommand(seeded.ReturnRequestId), CancellationToken.None);
        }

        await using (var receiptReadContext = new AppDbContext(options))
        {
            var received = await receiptReadContext.ReturnRequests.SingleAsync(request => request.Id == seeded.ReturnRequestId);
            var order = await receiptReadContext.Orders.SingleAsync(candidate => candidate.Id == seeded.OrderId);
            var variant = await receiptReadContext.ProductVariants.SingleAsync(candidate => candidate.Id == seeded.VariantId);
            received.Status.Should().Be(ReturnRequestStatus.Received);
            received.ApprovedAt.Should().BeNull();
            order.Status.Should().Be(OrderStatus.ReturnRequested);
            variant.Stock.Should().Be(2);
            var returnMovementCount = await receiptReadContext.StockMovements.CountAsync(movement =>
                movement.ReturnRequestId == seeded.ReturnRequestId);
            returnMovementCount.Should().Be(0);
        }

        var approveClock = new FixedClock(receiveClock.UtcNow.AddMinutes(5));
        await using (var approveContext = new AppDbContext(options))
        {
            var handler = new ApproveReturnRequestCommandHandler(
                new ReturnRequestRepository(approveContext),
                new OrderRepository(approveContext),
                new ReturnInventoryService(new ProductVariantRepository(approveContext)),
                approveClock,
                new UnitOfWork(approveContext));

            await handler.Handle(new ApproveReturnRequestCommand(seeded.ReturnRequestId, "Inspected"), CancellationToken.None);
        }

        await using var finalContext = new AppDbContext(options);
        var approved = await finalContext.ReturnRequests.SingleAsync(request => request.Id == seeded.ReturnRequestId);
        var finalOrder = await finalContext.Orders.SingleAsync(candidate => candidate.Id == seeded.OrderId);
        var finalVariant = await finalContext.ProductVariants.SingleAsync(candidate => candidate.Id == seeded.VariantId);
        var returnMovements = await finalContext.StockMovements
            .Where(movement => movement.ReturnRequestId == seeded.ReturnRequestId)
            .ToListAsync();

        approved.Status.Should().Be(ReturnRequestStatus.Approved);
        approved.ReceivedAt.Should().Be(receiveClock.UtcNow);
        approved.ApprovedAt.Should().Be(approveClock.UtcNow);
        finalOrder.Status.Should().Be(OrderStatus.Refunded);
        finalVariant.Stock.Should().Be(3);
        returnMovements.Should().ContainSingle(movement =>
            movement.Type == StockMovementType.SaleReturn && movement.QuantityDelta == 1);
    }

    // Burada iki yönetici aynı Received sürümünü değiştirirse ilk kararın kazanıp ikinci yazımın gerçek concurrency conflict olduğunu doğruluyorum.
    [Fact]
    public async Task Concurrent_Return_Decisions_Should_Allow_Only_One_Persisted_Mutation()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        var seeded = await SeedRequestedRefundAsync(options);
        var clock = new FixedClock(new DateTime(2026, 8, 23, 10, 0, 0, DateTimeKind.Utc));

        await using (var receiveContext = new AppDbContext(options))
        {
            var request = await receiveContext.ReturnRequests
                .Include(candidate => candidate.Items)
                .SingleAsync(candidate => candidate.Id == seeded.ReturnRequestId);
            request.Receive(clock.UtcNow);
            await receiveContext.SaveChangesAsync();
        }

        await using var firstContext = new AppDbContext(options);
        await using var secondContext = new AppDbContext(options);
        var first = await firstContext.ReturnRequests
            .Include(candidate => candidate.Items)
            .SingleAsync(candidate => candidate.Id == seeded.ReturnRequestId);
        var second = await secondContext.ReturnRequests
            .Include(candidate => candidate.Items)
            .SingleAsync(candidate => candidate.Id == seeded.ReturnRequestId);
        first.Approve(clock.UtcNow.AddMinutes(1), "First decision");
        second.Reject(clock.UtcNow.AddMinutes(1), "Second decision");

        await new UnitOfWork(firstContext).SaveChangesAsync();
        Func<Task> staleSave = () => new UnitOfWork(secondContext).SaveChangesAsync();

        await staleSave.Should().ThrowAsync<ConcurrencyException>();
        await using var readContext = new AppDbContext(options);
        var persisted = await readContext.ReturnRequests.SingleAsync(candidate => candidate.Id == seeded.ReturnRequestId);
        persisted.Status.Should().Be(ReturnRequestStatus.Approved);
        persisted.DecisionNote.Should().Be("First decision");
    }

    // Burada yinelenen stok hareketi hatasında exchange onayı, sipariş ve replacement çıkışının tamamen rollback olduğunu doğruluyorum.
    [Fact]
    public async Task Exchange_Approval_Should_Roll_Back_All_Changes_When_A_Stock_Movement_Is_Duplicated()
    {
        await using var connection = await CreateOpenConnectionAsync();
        var options = CreateOptions(connection);
        var seeded = await SeedReceivedExchangeWithExistingMovementAsync(options);
        var clock = new FixedClock(new DateTime(2026, 8, 23, 11, 0, 0, DateTimeKind.Utc));

        await using (var writeContext = new AppDbContext(options))
        {
            var handler = new ApproveReturnRequestCommandHandler(
                new ReturnRequestRepository(writeContext),
                new OrderRepository(writeContext),
                new ReturnInventoryService(new ProductVariantRepository(writeContext)),
                clock,
                new UnitOfWork(writeContext));

            Func<Task> approve = () => handler.Handle(
                new ApproveReturnRequestCommand(seeded.ReturnRequestId),
                CancellationToken.None);

            await approve.Should().ThrowAsync<DbUpdateException>();
        }

        await using var readContext = new AppDbContext(options);
        var request = await readContext.ReturnRequests.SingleAsync(candidate => candidate.Id == seeded.ReturnRequestId);
        var order = await readContext.Orders.SingleAsync(candidate => candidate.Id == seeded.OrderId);
        var original = await readContext.ProductVariants.SingleAsync(candidate => candidate.Id == seeded.OriginalVariantId);
        var replacement = await readContext.ProductVariants.SingleAsync(candidate => candidate.Id == seeded.ReplacementVariantId);
        var movements = await readContext.StockMovements
            .Where(movement => movement.ReturnRequestId == seeded.ReturnRequestId)
            .ToListAsync();

        request.Status.Should().Be(ReturnRequestStatus.Received);
        request.ApprovedAt.Should().BeNull();
        order.Status.Should().Be(OrderStatus.ReturnRequested);
        original.Stock.Should().Be(3);
        replacement.Stock.Should().Be(1);
        movements.Should().ContainSingle(movement => movement.Type == StockMovementType.SaleReturn);
        movements.Should().NotContain(movement => movement.Type == StockMovementType.Sale);
    }

    // Burada ilişkisel test için teslim edilmiş siparişe bağlı tek kalemli Requested refund kaydını hazırlıyorum.
    private static async Task<SeededReturn> SeedRequestedRefundAsync(DbContextOptions<AppDbContext> options)
    {
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var product = new Product("Return persistence product", "return-persistence-product", $"RET-MAIN-{Guid.NewGuid():N}");
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var variant = new ProductVariant(
            product.Id,
            "Original",
            $"RET-VARIANT-{Guid.NewGuid():N}",
            10m,
            2);
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        var order = new Order(null, $"ORD-{Guid.NewGuid():N}"[..30], 10m, 0m, 0m, 0m, 10m);
        var item = order.AddItem(product.Id, variant.Id, product.Title, variant.Sku, 10m, 1);
        order.EnsureItemsMatchSubTotal();
        var lifecycleTime = new DateTime(2026, 8, 22, 10, 0, 0, DateTimeKind.Utc);
        order.ChangeStatus(OrderStatus.Confirmed, lifecycleTime);
        var payment = new Payment(order.Id, PaymentProvider.Fake, order.GrandTotal, $"return_{Guid.NewGuid():N}");
        order.AddPayment(payment);
        payment.MarkAsPaid($"transaction_{Guid.NewGuid():N}");
        order.ChangeStatus(OrderStatus.Paid, lifecycleTime.AddMinutes(1));
        order.ChangeStatus(OrderStatus.Preparing, lifecycleTime.AddMinutes(2));
        order.ChangeStatus(OrderStatus.Shipped, lifecycleTime.AddMinutes(3));
        order.ChangeStatus(OrderStatus.Delivered, lifecycleTime.AddMinutes(4));
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var returnRequest = new ReturnRequest(order.Id, null, $"RET-{Guid.NewGuid():N}"[..30], ReturnType.Refund);
        returnRequest.AddItem(item, 1);
        order.MarkReturnRequested();
        context.ReturnRequests.Add(returnRequest);
        await context.SaveChangesAsync();
        return new SeededReturn(order.Id, variant.Id, returnRequest.Id);
    }

    // Burada rollback testi için teslim alınmış exchange kaydını ve idempotency indeksini tetikleyecek mevcut hareketi hazırlıyorum.
    private static async Task<SeededExchange> SeedReceivedExchangeWithExistingMovementAsync(
        DbContextOptions<AppDbContext> options)
    {
        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var product = new Product("Exchange persistence product", "exchange-persistence-product", $"EXC-MAIN-{Guid.NewGuid():N}");
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var original = new ProductVariant(product.Id, "Original", $"EXC-ORIGINAL-{Guid.NewGuid():N}", 10m, 2);
        var replacement = new ProductVariant(product.Id, "Replacement", $"EXC-REPLACEMENT-{Guid.NewGuid():N}", 10m, 1);
        context.ProductVariants.AddRange(original, replacement);
        await context.SaveChangesAsync();

        var order = new Order(null, $"ORD-{Guid.NewGuid():N}"[..30], 10m, 0m, 0m, 0m, 10m);
        var item = order.AddItem(product.Id, original.Id, product.Title, original.Sku, 10m, 1);
        order.EnsureItemsMatchSubTotal();
        var lifecycleTime = new DateTime(2026, 8, 22, 11, 0, 0, DateTimeKind.Utc);
        order.ChangeStatus(OrderStatus.Confirmed, lifecycleTime);
        var payment = new Payment(order.Id, PaymentProvider.Fake, order.GrandTotal, $"exchange_{Guid.NewGuid():N}");
        order.AddPayment(payment);
        payment.MarkAsPaid($"transaction_{Guid.NewGuid():N}");
        order.ChangeStatus(OrderStatus.Paid, lifecycleTime.AddMinutes(1));
        order.ChangeStatus(OrderStatus.Preparing, lifecycleTime.AddMinutes(2));
        order.ChangeStatus(OrderStatus.Shipped, lifecycleTime.AddMinutes(3));
        order.ChangeStatus(OrderStatus.Delivered, lifecycleTime.AddMinutes(4));
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var returnRequest = new ReturnRequest(order.Id, null, $"RET-{Guid.NewGuid():N}"[..30], ReturnType.Exchange);
        returnRequest.AddItem(item, 1, replacement.Id);
        returnRequest.Receive(lifecycleTime.AddMinutes(5));
        order.MarkReturnRequested();
        context.ReturnRequests.Add(returnRequest);
        await context.SaveChangesAsync();
        original.ApplyStockMovement(
            1,
            StockMovementType.SaleReturn,
            "Existing idempotent return movement.",
            order.Id,
            returnRequest.Id);
        await context.SaveChangesAsync();
        return new SeededExchange(order.Id, original.Id, replacement.Id, returnRequest.Id);
    }

    // Burada ilişkisel testlerin paylaşacağı açık SQLite bağlantısını oluşturuyorum.
    private static async Task<SqliteConnection> CreateOpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    // Burada tüm test context'lerini aynı bellek içi ilişkisel veritabanına bağlıyorum.
    private static DbContextOptions<AppDbContext> CreateOptions(SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
    }

    private sealed record SeededReturn(Guid OrderId, Guid VariantId, Guid ReturnRequestId);

    private sealed record SeededExchange(
        Guid OrderId,
        Guid OriginalVariantId,
        Guid ReplacementVariantId,
        Guid ReturnRequestId);

    private sealed class FixedClock : IDateTimeProvider
    {
        // Burada ilişkisel yaşam döngüsü testine sabit UTC saat sağlıyorum.
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
