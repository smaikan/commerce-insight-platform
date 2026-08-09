using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Commands.ImportOrders;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class ImportOrdersCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Import_Delivered_Order_And_Apply_Inventory_And_Metrics_When_Requested()
    {
        var product = new Product("Imported Product", "imported-product", "IMPORTED-MAIN").WithId(12);
        var variant = new ProductVariant(product.Id, "Default", "IMPORTED-SKU", 100m, 5);
        var orders = new Mock<IOrderRepository>();
        var users = new Mock<IUserRepository>();
        var variants = new Mock<IProductVariantRepository>();
        var products = new Mock<IProductRepository>();
        var metrics = new Mock<IOrderMetricsRecorder>();
        Order? savedOrder = null;

        orders.Setup(repository => repository.GetByOrderNumberAsync("EXT-1001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        orders.Setup(repository => repository.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => savedOrder = order)
            .Returns(Task.CompletedTask);
        users.Setup(repository => repository.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User("customer@example.com", "hash", "Customer", "One"));
        variants.Setup(repository => repository.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([variant]);
        products.Setup(repository => repository.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([product]);
        metrics.Setup(recorder => recorder.RecordPurchasedQuantitiesAsync(
                It.IsAny<IReadOnlyCollection<PurchaseMetricLine>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var unitOfWork = CreateTransactionalUnitOfWork();
        var processor = new ImportedOrderProcessor(
            orders.Object,
            users.Object,
            variants.Object,
            products.Object,
            Mock.Of<IShippingMethodRepository>(),
            metrics.Object,
            new FixedClock(),
            unitOfWork.Object);
        var handler = new ImportOrderCommandHandler(processor);
        var request = new ImportedOrderInput(
            "EXT-1001",
            7,
            100m,
            0m,
            0m,
            0m,
            100m,
            OrderStatus.Delivered,
            [new ImportedOrderItemInput(product.Id, variant.Id, "Historical title", "HIST-SKU", 100m, 1)],
            new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            PaymentProvider: PaymentProvider.Fake,
            PaymentTransactionId: "external-payment-1001",
            ApplyInventoryAndMetrics: true);

        var result = await handler.Handle(new ImportOrderCommand(request), CancellationToken.None);

        result.WasImported.Should().BeTrue();
        result.Order.Status.Should().Be(OrderStatus.Delivered);
        result.Order.CreatedAt.Should().Be(request.CreatedAtUtc);
        savedOrder.Should().NotBeNull();
        savedOrder!.Payments.Should().ContainSingle(payment => payment.Status == PaymentStatus.Paid);
        variant.Stock.Should().Be(4);
        variant.StockMovements.Should().ContainSingle(movement =>
            movement.Type == StockMovementType.Sale && movement.OrderId == savedOrder.Id);
        metrics.Verify(recorder => recorder.RecordPurchasedQuantitiesAsync(
            It.Is<IReadOnlyCollection<PurchaseMetricLine>>(lines =>
                lines.Single().Product == product && lines.Single().Variant == variant && lines.Single().Quantity == 1),
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_Existing_Order_Without_Changing_Stock()
    {
        var existing = new Order(7, "EXT-EXISTS", 100m, 0m, 0m, 0m, 100m);
        existing.AddItem(12, Guid.NewGuid(), "Product", "SKU", 100m, 1);
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByOrderNumberAsync("EXT-EXISTS", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var unitOfWork = CreateTransactionalUnitOfWork();
        var processor = new ImportedOrderProcessor(
            orders.Object,
            Mock.Of<IUserRepository>(),
            Mock.Of<IProductVariantRepository>(),
            Mock.Of<IProductRepository>(),
            Mock.Of<IShippingMethodRepository>(),
            Mock.Of<IOrderMetricsRecorder>(),
            new FixedClock(),
            unitOfWork.Object);

        var result = await new ImportOrderCommandHandler(processor).Handle(
            new ImportOrderCommand(new ImportedOrderInput(
                "EXT-EXISTS", 999, 100m, 0m, 0m, 0m, 100m, OrderStatus.Pending,
                [new ImportedOrderItemInput(12, Guid.NewGuid(), "Product", "SKU", 100m, 1)])),
            CancellationToken.None);

        result.WasImported.Should().BeFalse();
        result.Order.Id.Should().Be(existing.Id);
        orders.Verify(repository => repository.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IUnitOfWork> CreateTransactionalUnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.ExecuteInSerializableTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<OrderImportResultDto>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<OrderImportResultDto>>, CancellationToken>((operation, token) => operation(token));
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return unitOfWork;
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
    }
}
