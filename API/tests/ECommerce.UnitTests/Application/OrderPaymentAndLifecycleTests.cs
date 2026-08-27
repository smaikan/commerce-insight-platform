using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Payments;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Commands.CancelOrder;
using ECommerce.Application.Orders.Commands.ChangeOrderStatus;
using ECommerce.Application.Orders.Commands.CreatePayment;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Queries.GetMyOrders;
using ECommerce.Application.Orders.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class OrderPaymentAndLifecycleTests
{
    // Burada aynı idempotency anahtarıyla tekrarlanan ödeme isteğinin ikinci bir sağlayıcı çağrısı veya ödeme kaydı üretmediğini doğruluyorum.
    [Fact]
    public async Task CreatePayment_Should_Be_Idempotent_For_The_Same_Order_And_Key()
    {
        var order = CreateOrder();
        order.ChangeStatus(OrderStatus.Confirmed, new FixedClock().UtcNow);
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUserForUpdateAsync(
                order.Id,
                7,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var gateway = new RecordingPaymentGateway();
        var unitOfWork = CreateTransactionalUnitOfWork<PaymentDto>();
        var handler = new CreatePaymentCommandHandler(
            orders.Object,
            [gateway],
            new StubCurrentUser(7),
            new FixedClock(),
            unitOfWork.Object,
            Mock.Of<IAuthoritativeSalesMetricService>());
        var command = new CreatePaymentCommand(order.Id, PaymentProvider.Fake, "payment_retry_key_0001");

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        first.Id.Should().Be(second.Id);
        first.Status.Should().Be(PaymentStatus.Paid);
        order.Status.Should().Be(OrderStatus.Paid);
        order.Payments.Should().ContainSingle();
        gateway.CallCount.Should().Be(1);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // Burada ilk sağlayıcı çağrısı sürerken aynı anahtarın ikinci bir tahsilat çağrısı başlatmadığını doğruluyorum.
    [Fact]
    public async Task CreatePayment_Should_Return_Existing_Pending_Attempt_While_The_Gateway_Is_Processing()
    {
        var order = CreateOrder();
        order.ChangeStatus(OrderStatus.Confirmed, new FixedClock().UtcNow);
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUserForUpdateAsync(
                order.Id,
                7,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var gateway = new BlockingPaymentGateway();
        var handler = new CreatePaymentCommandHandler(
            orders.Object,
            [gateway],
            new StubCurrentUser(7),
            new FixedClock(),
            CreateTransactionalUnitOfWork<PaymentDto>().Object,
            Mock.Of<IAuthoritativeSalesMetricService>());
        var command = new CreatePaymentCommand(order.Id, PaymentProvider.Fake, "payment_pending_key_0001");

        var firstAttempt = handler.Handle(command, CancellationToken.None);
        await gateway.WaitUntilStartedAsync();
        var repeatedAttempt = await handler.Handle(command, CancellationToken.None);

        repeatedAttempt.Status.Should().Be(PaymentStatus.Pending);
        gateway.CallCount.Should().Be(1);
        gateway.Complete();
        var completedAttempt = await firstAttempt;

        completedAttempt.Status.Should().Be(PaymentStatus.Paid);
        order.Payments.Should().ContainSingle();
        gateway.CallCount.Should().Be(1);
    }

    // Burada müşteri açık ödeme formunu iptal ettiğinde Pending sonucun sipariş, ödeme ve stok rezervasyonunu birlikte kapattığını doğruluyorum.
    [Fact]
    public async Task CancelOrder_Should_Abandon_Pending_Checkout_Form_And_Release_Stock()
    {
        var (order, variant) = CreateOrderWithItem(stock: 3);
        variant.ApplyStockMovement(-1, StockMovementType.Sale, "Checkout reservation.", order.Id);
        order.StartStockReservation(new FixedClock().UtcNow, TimeSpan.FromMinutes(15));
        var payment = new Payment(order.Id, PaymentProvider.Iyzico, order.GrandTotal, "pending_payment_key_001");
        order.AddPayment(payment);
        payment.InitializeCheckoutForm(
            "pending-checkout-token-001",
            payment.Id.ToString("N"),
            "https://sandbox-cpp.iyzipay.com?token=pending-checkout-token-001",
            DateTime.UtcNow.AddMinutes(30));
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUserAsync(order.Id, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        orders.Setup(repository => repository.GetByIdForUserForUpdateAsync(order.Id, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        orders.Setup(repository => repository.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var variants = new Mock<IProductVariantRepository>();
        variants.Setup(repository => repository.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([variant]);
        var unitOfWork = CreateTransactionalUnitOfWork<OrderDto>();
        var reconciler = new Mock<IPendingPaymentCancellationReconciler>();
        reconciler.Setup(service => service.ReconcileForCancellationAsync(
                order,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PaymentStatus.Pending);
        var handler = new CancelOrderCommandHandler(
            orders.Object,
            new StubCurrentUser(7),
            CreateCancellationService(
                orders.Object,
                variants.Object,
                unitOfWork.Object,
                reconciler.Object));

        var result = await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        result.Status.Should().Be(OrderStatus.Cancelled);
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.ReservationExpiresAt.Should().BeNull();
        variant.Stock.Should().Be(3);
        variant.StockMovements.Should().ContainSingle(movement =>
            movement.Type == StockMovementType.Cancellation &&
            movement.QuantityDelta == 1 &&
            movement.OrderId == order.Id);
        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.CustomerAbandonedAt.Should().Be(new FixedClock().UtcNow);
        payment.AbandonmentReconciledAt.Should().BeNull();
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada sağlayıcının kesin başarısız saydığı ödeme iptal akışını zaten tamamladıysa müşteri tekrar iptalinde stok hareketinin yinelenmediğini doğruluyorum.
    [Fact]
    public async Task CancelOrder_Should_Return_Already_Cancelled_Order_After_Definitive_Reconciliation()
    {
        var (order, variant) = CreateOrderWithItem(stock: 3);
        var payment = new Payment(order.Id, PaymentProvider.Iyzico, order.GrandTotal, "pending_iyzico_cancel_key_001");
        order.AddPayment(payment);
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUserAsync(order.Id, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        orders.Setup(repository => repository.GetByIdForUserForUpdateAsync(order.Id, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        orders.Setup(repository => repository.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var reconciler = new Mock<IPendingPaymentCancellationReconciler>();
        reconciler.Setup(service => service.ReconcileForCancellationAsync(order, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                payment.MarkAsFailed("Provider rejected payment.");
                variant.ApplyStockMovement(1, StockMovementType.Cancellation, "Provider failure release.", order.Id);
                order.ChangeStatus(OrderStatus.Cancelled, new FixedClock().UtcNow);
                return PaymentStatus.Failed;
            });
        var unitOfWork = CreateTransactionalUnitOfWork<OrderDto>();
        var handler = new CancelOrderCommandHandler(
            orders.Object,
            new StubCurrentUser(7),
            CreateCancellationService(
                orders.Object,
                Mock.Of<IProductVariantRepository>(),
                unitOfWork.Object,
                reconciler.Object));

        var result = await handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        result.Status.Should().Be(OrderStatus.Cancelled);
        payment.Status.Should().Be(PaymentStatus.Failed);
        variant.StockMovements.Should().ContainSingle(movement => movement.Type == StockMovementType.Cancellation);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada provider uzlaştırması ödemeyi Paid sonuçlandırdığında müşteri iptalinin stok veya sipariş durumunu geri çevirmediğini doğruluyorum.
    [Fact]
    public async Task CancelOrder_Should_Reject_When_Reconciliation_Completes_The_Payment()
    {
        var (order, variant) = CreateOrderWithItem(stock: 3);
        var payment = new Payment(order.Id, PaymentProvider.Iyzico, order.GrandTotal, "paid_iyzico_cancel_key_001");
        order.AddPayment(payment);
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUserAsync(order.Id, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        orders.Setup(repository => repository.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var reconciler = new Mock<IPendingPaymentCancellationReconciler>();
        reconciler.Setup(service => service.ReconcileForCancellationAsync(order, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                payment.MarkAsPaid("provider-paid-id");
                order.ChangeStatus(OrderStatus.Confirmed, new FixedClock().UtcNow);
                order.ChangeStatus(OrderStatus.Paid, new FixedClock().UtcNow);
                return PaymentStatus.Paid;
            });
        var unitOfWork = CreateTransactionalUnitOfWork<OrderDto>();
        var handler = new CancelOrderCommandHandler(
            orders.Object,
            new StubCurrentUser(7),
            CreateCancellationService(
                orders.Object,
                Mock.Of<IProductVariantRepository>(),
                unitOfWork.Object,
                reconciler.Object));

        var action = () => handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        await action.Should().ThrowAsync<ApiContractException>();
        order.Status.Should().Be(OrderStatus.Paid);
        payment.Status.Should().Be(PaymentStatus.Paid);
        variant.StockMovements.Should().NotContain(movement => movement.Type == StockMovementType.Cancellation);
        orders.Verify(repository => repository.GetByIdForUserForUpdateAsync(
            It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada yöneticinin genel durum endpointiyle doğrudan refund akışını bypass edemediğini doğruluyorum.
    [Fact]
    public async Task ChangeStatus_Should_Reject_A_Direct_Refunded_Transition()
    {
        var order = CreateOrder();
        order.ChangeStatus(OrderStatus.Confirmed, new FixedClock().UtcNow);
        var payment = new Payment(order.Id, PaymentProvider.Fake, order.GrandTotal, "refund_payment_key_001");
        order.AddPayment(payment);
        payment.MarkAsPaid("fake_transaction_refund_001");
        order.ChangeStatus(OrderStatus.Paid, new FixedClock().UtcNow);
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var unitOfWork = CreateTransactionalUnitOfWork<OrderDto>();
        var handler = new ChangeOrderStatusCommandHandler(
            orders.Object,
            new OrderInventoryService(Mock.Of<IProductVariantRepository>()),
            new OrderCouponService(Mock.Of<ICouponRepository>(), new FixedClock()),
            new FixedClock(),
            unitOfWork.Object,
            Mock.Of<IOrderCancellationOperationRepository>());

        Func<Task> act = () => handler.Handle(
            new ChangeOrderStatusCommand(order.Id, OrderStatus.Refunded),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        order.Status.Should().Be(OrderStatus.Paid);
        payment.Status.Should().Be(PaymentStatus.Paid);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada genel sipariş durum komutunun Refunded değerini API doğrulama katmanında da reddettiğini doğruluyorum.
    [Fact]
    public void ChangeOrderStatusValidator_Should_Reject_Refunded()
    {
        var result = new ChangeOrderStatusCommandValidator().Validate(
            new ChangeOrderStatusCommand(Guid.NewGuid(), OrderStatus.Refunded));

        result.IsValid.Should().BeFalse();
    }

    // Burada kargoya verildi durumunun firma ve takip numarası olmadan kabul edilmediğini doğruluyorum.
    [Fact]
    public void ChangeOrderStatusValidator_Should_Require_Shipment_Fields_For_Shipped()
    {
        var validator = new ChangeOrderStatusCommandValidator();

        var missingFields = validator.Validate(new ChangeOrderStatusCommand(Guid.NewGuid(), OrderStatus.Shipped));
        var unsafeUrl = validator.Validate(new ChangeOrderStatusCommand(
            Guid.NewGuid(),
            OrderStatus.Shipped,
            "Carrier",
            "TRACK-123",
            "javascript:alert(1)"));

        missingFields.IsValid.Should().BeFalse();
        unsafeUrl.IsValid.Should().BeFalse();
    }

    // Burada yönetim durum komutunun kargo bilgilerini kaydedip DTO'ya kargoya çıkış tarihini yansıttığını doğruluyorum.
    [Fact]
    public async Task ChangeStatus_Should_Set_Shipment_Tracking_And_Shipped_Time()
    {
        var clock = new FixedClock();
        var order = CreateOrder();
        order.ChangeStatus(OrderStatus.Confirmed, clock.UtcNow);
        var payment = new Payment(order.Id, PaymentProvider.Fake, order.GrandTotal, "shipment_payment_key_001");
        order.AddPayment(payment);
        payment.MarkAsPaid("shipment_transaction_001");
        order.ChangeStatus(OrderStatus.Paid, clock.UtcNow);
        order.ChangeStatus(OrderStatus.Preparing, clock.UtcNow);
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var unitOfWork = CreateTransactionalUnitOfWork<OrderDto>();
        var handler = new ChangeOrderStatusCommandHandler(
            orders.Object,
            new OrderInventoryService(Mock.Of<IProductVariantRepository>()),
            new OrderCouponService(Mock.Of<ICouponRepository>(), clock),
            clock,
            unitOfWork.Object,
            Mock.Of<IOrderCancellationOperationRepository>());

        var result = await handler.Handle(
            new ChangeOrderStatusCommand(
                order.Id,
                OrderStatus.Shipped,
                "Carrier",
                "TRACK-123",
                "https://track.example.com/TRACK-123"),
            CancellationToken.None);

        result.Status.Should().Be(OrderStatus.Shipped);
        result.ShippingCarrier.Should().Be("Carrier");
        result.TrackingNumber.Should().Be("TRACK-123");
        result.TrackingUrl.Should().Be("https://track.example.com/TRACK-123");
        result.ShippedAt.Should().Be(clock.UtcNow);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada kalıcı cancellation intent'i bulunan Preparing siparişin admin tarafından Shipped yapılmadığını doğruluyorum.
    [Fact]
    public async Task ChangeStatus_Should_Reject_Shipment_While_Cancellation_Is_In_Progress()
    {
        var clock = new FixedClock();
        var order = CreateOrder();
        order.ChangeStatus(OrderStatus.Confirmed, clock.UtcNow);
        var payment = new Payment(order.Id, PaymentProvider.Iyzico, order.GrandTotal, "shipment_cancel_race_key_001");
        order.AddPayment(payment);
        payment.MarkAsPaid("shipment_cancel_provider_payment_001");
        order.ChangeStatus(OrderStatus.Paid, clock.UtcNow);
        order.ChangeStatus(OrderStatus.Preparing, clock.UtcNow);
        var operation = new OrderCancellationOperation(
            order,
            payment,
            OrderCancellationInitiatorType.Member,
            PaymentReversalType.Cancel,
            clock.UtcNow);
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var operations = new Mock<IOrderCancellationOperationRepository>();
        operations.Setup(repository => repository.GetByOrderIdAsync(
                order.Id,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(operation);
        var unitOfWork = CreateTransactionalUnitOfWork<OrderDto>();
        var handler = new ChangeOrderStatusCommandHandler(
            orders.Object,
            new OrderInventoryService(Mock.Of<IProductVariantRepository>()),
            new OrderCouponService(Mock.Of<ICouponRepository>(), clock),
            clock,
            unitOfWork.Object,
            operations.Object);

        var shipment = () => handler.Handle(
            new ChangeOrderStatusCommand(
                order.Id,
                OrderStatus.Shipped,
                "Carrier",
                "TRACK-RACE-001"),
            CancellationToken.None);

        var exception = await shipment.Should().ThrowAsync<ApiContractException>();
        exception.Which.ErrorCode.Should().Be("order_cancellation_in_progress");
        order.Status.Should().Be(OrderStatus.Preparing);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada yönetici kupon kodunu değiştirse bile iptal sırasında kullanım kaydının sipariş kimliğiyle geri alındığını doğruluyorum.
    [Fact]
    public async Task ReleaseCoupon_Should_Use_CouponUsage_Order_Link_After_A_Code_Change()
    {
        var clock = new FixedClock();
        var coupon = new Coupon("OLD-CODE", CouponDiscountType.FixedAmount, 10m);
        coupon.IncreaseUsedCount(clock.UtcNow);
        coupon.Update(
            "NEW-CODE",
            CouponDiscountType.FixedAmount,
            10m,
            null,
            null,
            null,
            null,
            null);
        var order = new Order(
            7,
            $"ORD-{Guid.NewGuid():N}"[..24],
            10m,
            10m,
            0m,
            0m,
            0m,
            couponCode: "OLD-CODE");
        var usage = new CouponUsage(coupon.Id, 7, order.Id, clock.UtcNow);
        var coupons = new Mock<ICouponRepository>();
        coupons.Setup(repository => repository.GetUsageByOrderForUpdateAsync(
                order.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(usage);
        coupons.Setup(repository => repository.GetByIdForUpdateAsync(
                coupon.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);
        var service = new OrderCouponService(coupons.Object, clock);

        await service.ReleaseForCancellationAsync(order, CancellationToken.None);

        coupon.UsedCount.Should().Be(0);
        coupons.Verify(repository => repository.RemoveUsage(usage), Times.Once);
    }

    // Burada çok büyük sayfa numarasının pahalı veya taşan offset sorgusuna dönüşmeden validator tarafından reddedildiğini doğruluyorum.
    [Fact]
    public void GetMyOrdersValidator_Should_Reject_An_Excessive_Page_Number()
    {
        var result = new GetMyOrdersQueryValidator().Validate(new GetMyOrdersQuery(10_001));

        result.IsValid.Should().BeFalse();
    }

    // Burada test için ödeme yapılabilir, boş kalem grafiği gerektirmeyen temel siparişi oluşturuyorum.
    private static Order CreateOrder()
    {
        return new Order(7, $"ORD-{Guid.NewGuid():N}"[..24], 100m, 0m, 0m, 0m, 100m);
    }

    // Burada iptal testinin stok geri yükleme davranışı için ürün varyantı içeren siparişi oluşturuyorum.
    private static (Order Order, ProductVariant Variant) CreateOrderWithItem(int stock)
    {
        var product = new Product("Order Product", "order-product-lifecycle", "ORDER-LIFECYCLE", status: ProductStatus.Active)
            .WithId(12);
        var variant = new ProductVariant(product.Id, "Default", "ORDER-LIFECYCLE-SKU", 10m, stock);
        var order = new Order(7, $"ORD-{Guid.NewGuid():N}"[..24], 10m, 0m, 0m, 0m, 10m);
        order.AddItem(product.Id, variant.Id, product.Title, variant.Sku, 10m, 1);
        order.EnsureItemsMatchSubTotal();
        return (order, variant);
    }

    // Burada serializable transaction delegesini gerçek çağıran ve kaydetmeyi taklit eden generic unit of work mockunu hazırlıyorum.
    private static Mock<IUnitOfWork> CreateTransactionalUnitOfWork<TResponse>()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.ExecuteInSerializableTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<TResponse>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<TResponse>>, CancellationToken>((operation, token) => operation(token));
        unitOfWork.Setup(unit => unit.ExecuteInSerializableTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<bool>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<bool>>, CancellationToken>((operation, token) => operation(token));
        unitOfWork.Setup(unit => unit.ExecuteInSerializableTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<OrderCancellationOperation>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<OrderCancellationOperation>>, CancellationToken>((operation, token) => operation(token));
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return unitOfWork;
    }

    // Burada eski lifecycle testlerini yeni ortak cancellation sagasının tahsilatsız dalıyla çalıştırıyorum.
    private static OrderCancellationService CreateCancellationService(
        IOrderRepository orders,
        IProductVariantRepository variants,
        IUnitOfWork unitOfWork,
        IPendingPaymentCancellationReconciler reconciler)
    {
        return new OrderCancellationService(
            orders,
            Mock.Of<IOrderCancellationOperationRepository>(),
            reconciler,
            Mock.Of<ICheckoutFormGateway>(),
            new OrderInventoryService(variants),
            new OrderCouponService(Mock.Of<ICouponRepository>(), new FixedClock()),
            Mock.Of<IOrderNotificationService>(),
            new FixedClock(),
            unitOfWork,
            Mock.Of<IAuthoritativeSalesMetricService>());
    }

    private sealed class StubCurrentUser : ICurrentUserService
    {
        // Burada test akışlarının sabit kullanıcı kimliğini hazırlıyorum.
        public StubCurrentUser(long userId)
        {
            UserId = userId;
        }

        public long? UserId { get; }
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class RecordingPaymentGateway : IPaymentGateway
    {
        public PaymentProvider Provider => PaymentProvider.Fake;
        public int CallCount { get; private set; }

        // Burada ödeme sağlayıcısı çağrısını kaydedip güvenli sahte başarı sonucu döndürüyorum.
        public Task<PaymentGatewayResult> ChargeAsync(
            PaymentGatewayRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new PaymentGatewayResult(true, "fake_transaction_payment_001", null));
        }
    }

    private sealed class BlockingPaymentGateway : IPaymentGateway
    {
        private readonly TaskCompletionSource<bool> _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<PaymentGatewayResult> _completion = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public PaymentProvider Provider => PaymentProvider.Fake;
        public int CallCount { get; private set; }

        // Burada dış ödeme çağrısının başladığını bildirip test tamamlayana kadar sonucu bekletiyorum.
        public Task<PaymentGatewayResult> ChargeAsync(
            PaymentGatewayRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            _started.TrySetResult(true);
            return _completion.Task;
        }

        // Burada ilk ödeme çağrısının transaction dışına çıktığını testin güvenle beklemesini sağlıyorum.
        public Task WaitUntilStartedAsync()
        {
            return _started.Task;
        }

        // Burada bekleyen sahte sağlayıcı çağrısını başarılı sonuçla tamamlıyorum.
        public void Complete()
        {
            _completion.TrySetResult(new PaymentGatewayResult(
                true,
                "fake_transaction_pending_001",
                null));
        }
    }
}
