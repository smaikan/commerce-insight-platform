using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Payments;
using ECommerce.Application.Common.Security;
using ECommerce.Application.GuestOrders;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class GuestOrderCancellationTests
{
    // Burada guest müşterinin Pending CheckoutForm oturumunu iptal ederek sipariş, ödeme ve stok rezervasyonunu birlikte kapattığını doğruluyorum.
    [Fact]
    public async Task CancelAsync_Should_Abandon_Pending_Checkout_Form_And_Release_Stock()
    {
        var clock = new FixedClock();
        var product = new Product("Guest Product", "guest-product", "GUEST-PRODUCT", status: ProductStatus.Active)
            .WithId(73);
        var variant = new ProductVariant(product.Id, "Default", "GUEST-SKU", 100m, 2);
        var order = new Order(null, $"ORD-{Guid.NewGuid():N}"[..24], 100m, 0m, 0m, 0m, 100m);
        order.AddItem(product.Id, variant.Id, product.Title, variant.Sku, 100m, 1);
        order.EnsureItemsMatchSubTotal();
        variant.ApplyStockMovement(-1, StockMovementType.Sale, "Guest checkout reservation.", order.Id);
        order.StartStockReservation(clock.UtcNow, TimeSpan.FromMinutes(15));
        var payment = new Payment(order.Id, PaymentProvider.Iyzico, order.GrandTotal, "guest_pending_payment_001");
        order.AddPayment(payment);
        payment.InitializeCheckoutForm(
            "guest-checkout-token-001",
            payment.Id.ToString("N"),
            "https://sandbox-cpp.iyzipay.com?token=guest-checkout-token-001",
            DateTime.UtcNow.AddMinutes(30));

        var hash = new string('A', GuestOrderSession.Sha256HexLength);
        var session = new GuestOrderSession(hash, hash, clock.UtcNow, clock.UtcNow.AddDays(1));
        var guestOrders = new Mock<IGuestOrderRepository>();
        guestOrders.Setup(repository => repository.GetSessionForUpdateAsync(hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        guestOrders.Setup(repository => repository.GetOrderForSessionAsync(
                session.Id,
                order.Id,
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var tokens = new Mock<IGuestTokenService>();
        tokens.Setup(service => service.Hash(It.IsAny<string>())).Returns(hash);
        var unitOfWork = CreateUnitOfWork();
        var access = new GuestOrderAccessService(
            guestOrders.Object,
            Mock.Of<IEmailOutboxRepository>(),
            Mock.Of<IUserRepository>(),
            tokens.Object,
            Mock.Of<IGuestOrderAccessTokenProtector>(),
            Mock.Of<IGuestCheckoutProtectionService>(),
            clock,
            unitOfWork.Object);
        var variants = new Mock<IProductVariantRepository>();
        variants.Setup(repository => repository.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([variant]);
        var reconciler = new Mock<IPendingPaymentCancellationReconciler>();
        reconciler.Setup(service => service.ReconcileForCancellationAsync(order, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PaymentStatus.Pending);
        var notifications = new Mock<IOrderNotificationService>();
        notifications.Setup(service => service.QueueOrderStatusChangedAsync(order, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = new GuestOrderOperationsService(
            access,
            guestOrders.Object,
            Mock.Of<IOrderRepository>(),
            Mock.Of<IReturnRequestRepository>(),
            variants.Object,
            [],
            notifications.Object,
            clock,
            unitOfWork.Object,
            new OrderCancellationService(
                MockGuestOrderRepository(order),
                Mock.Of<IOrderCancellationOperationRepository>(),
                reconciler.Object,
                Mock.Of<ICheckoutFormGateway>(),
                new OrderInventoryService(variants.Object),
                new OrderCouponService(Mock.Of<ICouponRepository>(), clock),
                notifications.Object,
                clock,
                unitOfWork.Object));

        var result = await service.CancelAsync("guest-session", "guest-csrf", order.Id, CancellationToken.None);

        result.Status.Should().Be(OrderStatus.Cancelled);
        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.CustomerAbandonedAt.Should().Be(clock.UtcNow);
        order.ReservationExpiresAt.Should().BeNull();
        variant.Stock.Should().Be(2);
        variant.StockMovements.Should().ContainSingle(movement =>
            movement.Type == StockMovementType.Cancellation && movement.OrderId == order.Id);
    }

    // Burada guest iptal testinin serializable transaction delegesini gerçek çağıran unit of work mockunu hazırlıyorum.
    private static Mock<IUnitOfWork> CreateUnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.ExecuteInSerializableTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<OrderDto>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<OrderDto>>, CancellationToken>((operation, token) => operation(token));
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return unitOfWork;
    }

    // Burada ortak cancellation servisinin generic order update sorgusunu guest sipariş aggregate'ına bağlıyorum.
    private static IOrderRepository MockGuestOrderRepository(Order order)
    {
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        orders.Setup(repository => repository.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        return orders.Object;
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 8, 24, 5, 0, 0, DateTimeKind.Utc);
    }
}
