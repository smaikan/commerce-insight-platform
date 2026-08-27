using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Common.Payments;
using ECommerce.Application.Common.Security;
using ECommerce.Application.GuestOrders;
using ECommerce.Application.Orders.Services;
using ECommerce.Application.Payments;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class IyzicoCheckoutFormPaymentServiceTests
{
    // Burada Pending kaydın provider çağrısından önce kaydedildiğini ve iyzico form oturumunun sonradan bağlandığını doğruluyorum.
    [Fact]
    public async Task InitializeForCurrentUserAsync_Should_Create_Pending_Then_Persist_Form_Session()
    {
        var order = CreatePayableOrder();
        var repository = CreateOrderRepository(order);
        var gateway = new RecordingCheckoutFormGateway();
        var unitOfWork = CreateUnitOfWork();
        var service = CreateService(repository.Object, gateway, unitOfWork.Object);

        var result = await service.InitializeForCurrentUserAsync(
            order.Id,
            "iyzico_application_key_0001",
            "127.0.0.1",
            CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Pending);
        result.PaymentPageUrl.Should().Be("https://sandbox-api.iyzipay.com/checkoutform/test-token");
        order.Status.Should().Be(OrderStatus.Pending);
        order.Payments.Should().ContainSingle(payment =>
            payment.Provider == PaymentProvider.Iyzico &&
            payment.ProviderToken == "test-token");
        gateway.InitializeCallCount.Should().Be(1);
        gateway.LastInitializeRequest.Should().NotBeNull();
        gateway.LastInitializeRequest!.Price.Should().Be(order.SubTotal);
        gateway.LastInitializeRequest.PaidPrice.Should().Be(order.GrandTotal);
        gateway.LastInitializeRequest.Items.Should().ContainSingle();
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // Burada imzalı retrieve sonucu yerel siparişle eşleştiğinde ödeme ve siparişin atomik Paid olduğunu doğruluyorum.
    [Fact]
    public async Task CompleteByTokenAsync_Should_Mark_Matching_Payment_And_Order_As_Paid()
    {
        var order = CreatePayableOrder();
        order.ChangeStatus(OrderStatus.Confirmed, DateTime.UtcNow);
        var payment = new Payment(
            order.Id,
            PaymentProvider.Iyzico,
            order.GrandTotal,
            "iyzico_application_key_0002");
        order.AddPayment(payment);
        payment.InitializeCheckoutForm(
            "test-token",
            payment.Id.ToString("N"),
            "https://sandbox-api.iyzipay.com/checkoutform/test-token",
            DateTime.UtcNow.AddMinutes(30));
        var repository = CreateOrderRepository(order);
        var gateway = new RecordingCheckoutFormGateway
        {
            RetrieveResult = new CheckoutFormRetrieveResult(
                CheckoutFormPaymentState.Paid,
                "test-token",
                payment.Id.ToString("N"),
                order.Id.ToString("N"),
                "TRY",
                order.SubTotal,
                order.GrandTotal,
                1,
                "28157797",
                1,
                null,
                CreateProviderItems(order, order.GrandTotal))
        };
        var unitOfWork = CreateUnitOfWork();
        var cart = Cart.CreateForUser(7);
        cart.AddItem(1, Guid.NewGuid(), 1, 100m);
        var carts = new Mock<ICartRepository>();
        carts.Setup(item => item.GetByOwnerForUpdateAsync(
                It.Is<CartOwner>(owner => owner.UserId == 7),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        var salesMetrics = new Mock<IAuthoritativeSalesMetricService>();
        var service = CreateService(
            repository.Object,
            gateway,
            unitOfWork.Object,
            carts: carts.Object,
            salesMetrics: salesMetrics.Object);

        var result = await service.CompleteByTokenAsync("test-token", CancellationToken.None);
        var replay = await service.CompleteByTokenAsync("test-token", CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Paid);
        payment.Status.Should().Be(PaymentStatus.Paid);
        payment.TransactionId.Should().Be("28157797");
        payment.FraudStatus.Should().Be(1);
        payment.ProviderPaidAmount.Should().Be(order.GrandTotal);
        payment.InstallmentCount.Should().Be(1);
        order.Status.Should().Be(OrderStatus.Paid);
        cart.IsEmpty.Should().BeTrue();
        gateway.RetrieveCallCount.Should().Be(1);
        gateway.LastRetrieveConversationId.Should().Be(payment.Id.ToString("N"));
        replay.Status.Should().Be(PaymentStatus.Paid);
        salesMetrics.Verify(metric => metric.RecordPaidOrderAsync(
            order,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada iyzico'nun kalemlere sekiz basamakla dağıttığı tutarın kuruşa dengelenip başarılı ödemeyi reddetmediğini doğruluyorum.
    [Fact]
    public async Task CompleteByTokenAsync_Should_Normalize_Provider_Item_Rounding_Without_Losing_Total()
    {
        var order = CreatePayableOrderWithTwoItems();
        var payment = AddInitializedPayment(order, "iyzico_item_rounding_01");
        var providerItems = order.Items
            .Select(item => new CheckoutFormItemTransaction(
                $"provider-item-{item.Id:N}",
                item.Id.ToString("N"),
                item.TotalPrice,
                973.99999998m,
                2))
            .ToList();
        var gateway = new RecordingCheckoutFormGateway
        {
            RetrieveResult = new CheckoutFormRetrieveResult(
                CheckoutFormPaymentState.Paid,
                "test-token",
                payment.Id.ToString("N"),
                order.Id.ToString("N"),
                "TRY",
                1498.34m,
                1948.00m,
                1,
                "provider-paid-rounding-id",
                1,
                null,
                providerItems)
        };
        var service = CreateService(
            CreateOrderRepository(order).Object,
            gateway,
            CreateUnitOfWork().Object);

        var result = await service.CompleteByTokenAsync("test-token", CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Paid);
        order.Status.Should().Be(OrderStatus.Paid);
        payment.ProviderPaidAmount.Should().Be(1948.00m);
        payment.ItemTransactions.Should().HaveCount(2);
        payment.ItemTransactions.Should().OnlyContain(item => item.PaidPrice == 974.00m);
        payment.ItemTransactions.Sum(item => item.PaidPrice).Should().Be(1948.00m);
    }

    // Burada aynı idempotency anahtarının ikinci provider oturumu üretmeden mevcut formu döndürdüğünü doğruluyorum.
    [Fact]
    public async Task InitializeForCurrentUserAsync_Should_Return_Existing_Session_For_Same_Key()
    {
        var order = CreatePayableOrder();
        var repository = CreateOrderRepository(order);
        var gateway = new RecordingCheckoutFormGateway();
        var unitOfWork = CreateUnitOfWork();
        var service = CreateService(repository.Object, gateway, unitOfWork.Object);

        var first = await service.InitializeForCurrentUserAsync(
            order.Id, "iyzico_application_key_0003", "127.0.0.1", CancellationToken.None);
        var second = await service.InitializeForCurrentUserAsync(
            order.Id, "iyzico_application_key_0003", "127.0.0.1", CancellationToken.None);

        second.Should().BeEquivalentTo(first);
        order.Payments.Should().ContainSingle();
        gateway.InitializeCallCount.Should().Be(1);
    }

    // Burada provider sepet tutarı siparişle eşleşmediğinde yerel ödeme ve siparişin değişmediğini doğruluyorum.
    [Fact]
    public async Task CompleteByTokenAsync_Should_Reject_Basket_Price_Mismatch_Without_Mutation()
    {
        var order = CreatePayableOrder();
        order.ChangeStatus(OrderStatus.Confirmed, DateTime.UtcNow);
        var payment = new Payment(
            order.Id,
            PaymentProvider.Iyzico,
            order.GrandTotal,
            "iyzico_application_key_0004");
        order.AddPayment(payment);
        payment.InitializeCheckoutForm(
            "test-token",
            payment.Id.ToString("N"),
            "https://sandbox-api.iyzipay.com/checkoutform/test-token",
            DateTime.UtcNow.AddMinutes(30));
        var repository = CreateOrderRepository(order);
        var gateway = new RecordingCheckoutFormGateway
        {
            RetrieveResult = new CheckoutFormRetrieveResult(
                CheckoutFormPaymentState.Paid,
                "test-token",
                payment.Id.ToString("N"),
                order.Id.ToString("N"),
                "TRY",
                order.SubTotal + 1m,
                order.GrandTotal,
                1,
                "28157798",
                1,
                null)
        };
        var service = CreateService(repository.Object, gateway, CreateUnitOfWork().Object);

        var action = () => service.CompleteByTokenAsync("test-token", CancellationToken.None);

        await action.Should().ThrowAsync<ECommerce.Application.Common.Exceptions.ConflictException>();
        payment.Status.Should().Be(PaymentStatus.Pending);
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    // Burada taksit farkı eklenmiş imzalı tahsilat tutarının ödeme ve siparişi Paid yaptığını doğruluyorum.
    [Fact]
    public async Task CompleteByTokenAsync_Should_Accept_Installment_Surcharge_And_Record_Provider_Charge()
    {
        var order = CreatePayableOrder();
        order.ChangeStatus(OrderStatus.Confirmed, DateTime.UtcNow);
        var payment = new Payment(
            order.Id,
            PaymentProvider.Iyzico,
            order.GrandTotal,
            "iyzico_application_key_installment");
        order.AddPayment(payment);
        payment.InitializeCheckoutForm(
            "test-token",
            payment.Id.ToString("N"),
            "https://sandbox-api.iyzipay.com/checkoutform/test-token",
            DateTime.UtcNow.AddMinutes(30));
        var repository = CreateOrderRepository(order);
        var gateway = new RecordingCheckoutFormGateway
        {
            RetrieveResult = new CheckoutFormRetrieveResult(
                CheckoutFormPaymentState.Paid,
                "test-token",
                payment.Id.ToString("N"),
                order.Id.ToString("N"),
                "TRY",
                order.SubTotal,
                112.25m,
                6,
                "28157799",
                1,
                null,
                CreateProviderItems(order, 112.25m))
        };
        var service = CreateService(repository.Object, gateway, CreateUnitOfWork().Object);

        var result = await service.CompleteByTokenAsync("test-token", CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Paid);
        payment.ProviderPaidAmount.Should().Be(112.25m);
        payment.InstallmentCount.Should().Be(6);
        order.Status.Should().Be(OrderStatus.Paid);
    }

    // Burada bağlantı kopmasından kalan tokensız Pending kaydın aynı key ile yeni Payment oluşturmadan iyileştiğini doğruluyorum.
    [Fact]
    public async Task InitializeForCurrentUserAsync_Should_Resume_Tokenless_Pending_Attempt()
    {
        var order = CreatePayableOrder();
        order.ChangeStatus(OrderStatus.Confirmed, DateTime.UtcNow);
        var payment = new Payment(
            order.Id,
            PaymentProvider.Iyzico,
            order.GrandTotal,
            "iyzico_application_key_0005");
        order.AddPayment(payment);
        var repository = CreateOrderRepository(order);
        var gateway = new RecordingCheckoutFormGateway();
        var service = CreateService(repository.Object, gateway, CreateUnitOfWork().Object);

        var result = await service.InitializeForCurrentUserAsync(
            order.Id, "iyzico_application_key_0005", "127.0.0.1", CancellationToken.None);

        result.PaymentId.Should().Be(payment.Id);
        result.PaymentPageUrl.Should().NotBeNull();
        order.Payments.Should().ContainSingle();
        gateway.InitializeCallCount.Should().Be(1);
        repository.Verify(item => item.AddPaymentAsync(
            It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada doğrulanmış kesin başarısız callback'in ödeme, sipariş ve stoğu tek kez kapatıp sepeti koruduğunu doğruluyorum.
    [Fact]
    public async Task CompleteByTokenAsync_Should_Cancel_And_Release_Reservation_On_Definitive_Failure()
    {
        var (order, variant) = CreateReservedPayableOrder();
        var payment = AddInitializedPayment(order, "iyzico_application_key_failure_01");
        var repository = CreateOrderRepository(order);
        var variants = new Mock<IProductVariantRepository>();
        variants.Setup(item => item.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([variant]);
        var carts = new Mock<ICartRepository>();
        var gateway = new RecordingCheckoutFormGateway
        {
            RetrieveResult = CreateRetrieveResult(order, payment, CheckoutFormPaymentState.Failed, -1)
        };
        var service = CreateService(
            repository.Object,
            gateway,
            CreateUnitOfWork().Object,
            variants.Object,
            carts: carts.Object);

        var first = await service.CompleteByTokenAsync("test-token", CancellationToken.None);
        var replay = await service.CompleteByTokenAsync("test-token", CancellationToken.None);

        first.Status.Should().Be(PaymentStatus.Failed);
        replay.Status.Should().Be(PaymentStatus.Failed);
        payment.Status.Should().Be(PaymentStatus.Failed);
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.ReservationExpiresAt.Should().BeNull();
        variant.Stock.Should().Be(2);
        variant.StockMovements.Should().ContainSingle(movement =>
            movement.Type == StockMovementType.Cancellation && movement.OrderId == order.Id);
        gateway.RetrieveCallCount.Should().Be(1);
        carts.Verify(item => item.GetByOwnerForUpdateAsync(
            It.IsAny<CartOwner>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada fraud incelemesindeki belirsiz sonucun ödeme ve rezervasyonu açık bıraktığını doğruluyorum.
    [Fact]
    public async Task CompleteByTokenAsync_Should_Keep_Reservation_For_FraudStatus_Zero()
    {
        var (order, variant) = CreateReservedPayableOrder();
        var payment = AddInitializedPayment(order, "iyzico_application_key_pending_01");
        var repository = CreateOrderRepository(order);
        var gateway = new RecordingCheckoutFormGateway
        {
            RetrieveResult = CreateRetrieveResult(order, payment, CheckoutFormPaymentState.Pending, 0)
        };
        var service = CreateService(repository.Object, gateway, CreateUnitOfWork().Object);

        var result = await service.CompleteByTokenAsync("test-token", CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Pending);
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.FraudStatus.Should().Be(0);
        order.Status.Should().Be(OrderStatus.Pending);
        order.ReservationExpiresAt.Should().NotBeNull();
        variant.Stock.Should().Be(1);
        variant.StockMovements.Should().NotContain(movement => movement.Type == StockMovementType.Cancellation);
    }

    // Burada müşteri iptal ön kontrolünün fraud incelemesindeki belirsiz iyzico sonucunda Pending döndürüp rezervasyonu koruduğunu doğruluyorum.
    [Fact]
    public async Task ReconcileForCancellationAsync_Should_Keep_Unknown_Payment_Pending()
    {
        var (order, variant) = CreateReservedPayableOrder();
        var payment = AddInitializedPayment(order, "iyzico_cancel_reconcile_pending_01");
        var repository = CreateOrderRepository(order);
        var gateway = new RecordingCheckoutFormGateway
        {
            RetrieveResult = CreateRetrieveResult(order, payment, CheckoutFormPaymentState.Pending, 0)
        };
        var service = CreateService(repository.Object, gateway, CreateUnitOfWork().Object);

        var result = await service.ReconcileForCancellationAsync(order, CancellationToken.None);

        result.Should().Be(PaymentStatus.Pending);
        order.Status.Should().Be(OrderStatus.Pending);
        payment.Status.Should().Be(PaymentStatus.Pending);
        variant.Stock.Should().Be(1);
        variant.StockMovements.Should().NotContain(movement => movement.Type == StockMovementType.Cancellation);
    }

    // Burada müşteri iptalinden sonra geç gelen Paid sonucunun siparişi diriltmeden iyzico ters işlemiyle kapatıldığını doğruluyorum.
    [Fact]
    public async Task CompleteByTokenAsync_Should_Reverse_Late_Charge_For_Abandoned_Checkout_Form()
    {
        var order = CreatePayableOrder();
        var payment = AddInitializedPayment(order, "iyzico_abandoned_late_charge_01");
        payment.AbandonCheckoutForm(new FixedClock().UtcNow);
        order.ChangeStatus(OrderStatus.Cancelled, new FixedClock().UtcNow);
        var repository = CreateOrderRepository(order);
        var gateway = new RecordingCheckoutFormGateway
        {
            RetrieveResult = CreateRetrieveResult(order, payment, CheckoutFormPaymentState.Paid, 1),
            ReversalResult = new LatePaymentReversalResult(true, false)
        };
        var service = CreateService(repository.Object, gateway, CreateUnitOfWork().Object);

        var result = await service.CompleteByTokenAsync("test-token", CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Cancelled);
        order.Status.Should().Be(OrderStatus.Cancelled);
        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.TransactionId.Should().Be("provider-paid-id");
        payment.LateChargeReversedAt.Should().Be(new FixedClock().UtcNow);
        payment.AbandonmentReconciledAt.Should().Be(new FixedClock().UtcNow);
        gateway.RetrieveCallCount.Should().Be(1);
        gateway.ReversalCallCount.Should().Be(1);
    }

    // Burada iptal ön kontrolünün sağlayıcıda tamamlanmış ödemeyi Paid olarak sonuçlandırıp sipariş iptaline izin vermediğini doğruluyorum.
    [Fact]
    public async Task ReconcileForCancellationAsync_Should_Return_Paid_When_Provider_Already_Charged()
    {
        var order = CreatePayableOrder();
        var payment = AddInitializedPayment(order, "iyzico_cancel_reconcile_paid_01");
        var repository = CreateOrderRepository(order);
        var gateway = new RecordingCheckoutFormGateway
        {
            RetrieveResult = CreateRetrieveResult(order, payment, CheckoutFormPaymentState.Paid, 1)
        };
        var service = CreateService(repository.Object, gateway, CreateUnitOfWork().Object);

        var result = await service.ReconcileForCancellationAsync(order, CancellationToken.None);

        result.Should().Be(PaymentStatus.Paid);
        order.Status.Should().Be(OrderStatus.Paid);
        payment.Status.Should().Be(PaymentStatus.Paid);
    }

    // Burada provider iletişim hatasının iptal ön kontrolünde 409'a dönüştüğünü ve stok rezervasyonunu değiştirmediğini doğruluyorum.
    [Fact]
    public async Task ReconcileForCancellationAsync_Should_Reject_Connectivity_Error_Without_Releasing_Stock()
    {
        var (order, variant) = CreateReservedPayableOrder();
        var payment = AddInitializedPayment(order, "iyzico_cancel_reconcile_unknown_01");
        var repository = CreateOrderRepository(order);
        var gateway = new RecordingCheckoutFormGateway
        {
            RetrieveException = new HttpRequestException("provider unavailable")
        };
        var service = CreateService(repository.Object, gateway, CreateUnitOfWork().Object);

        var action = () => service.ReconcileForCancellationAsync(order, CancellationToken.None);

        await action.Should().ThrowAsync<ECommerce.Application.Common.Exceptions.ConflictException>();
        order.Status.Should().Be(OrderStatus.Pending);
        payment.Status.Should().Be(PaymentStatus.Pending);
        variant.Stock.Should().Be(1);
        variant.StockMovements.Should().NotContain(movement => movement.Type == StockMovementType.Cancellation);
    }

    // Burada iyzico tokena ait ödeme kaydı bulunmadığını bildirdiğinde aktif oturumda da müşteri iptalinin devam edebildiğini doğruluyorum.
    [Fact]
    public async Task ReconcileForCancellationAsync_Should_Allow_Cancellation_When_Provider_Has_No_Payment_Record()
    {
        var order = CreatePayableOrder();
        var payment = AddInitializedPayment(order, "iyzico_cancel_expired_token_01");
        var repository = CreateOrderRepository(order);
        var gateway = new RecordingCheckoutFormGateway
        {
            RetrieveException = new CheckoutFormProviderUnavailableException("expired token", "5122")
        };
        var service = CreateService(
            repository.Object,
            gateway,
            CreateUnitOfWork().Object);

        var result = await service.ReconcileForCancellationAsync(order, CancellationToken.None);

        result.Should().Be(PaymentStatus.Pending);
        order.Status.Should().Be(OrderStatus.Pending);
        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    // Burada token zamanı ne olursa olsun 5122 dışındaki provider hatalarının finansal güvenlik nedeniyle iptale açılmadığını doğruluyorum.
    [Fact]
    public async Task ReconcileForCancellationAsync_Should_Reject_Other_Provider_Error()
    {
        var order = CreatePayableOrder();
        var payment = AddInitializedPayment(order, "iyzico_cancel_expired_auth_error_01");
        var repository = CreateOrderRepository(order);
        var gateway = new RecordingCheckoutFormGateway
        {
            RetrieveException = new CheckoutFormProviderUnavailableException("authentication error", "1000")
        };
        var service = CreateService(
            repository.Object,
            gateway,
            CreateUnitOfWork().Object);

        var action = () => service.ReconcileForCancellationAsync(order, CancellationToken.None);

        await action.Should().ThrowAsync<ECommerce.Application.Common.Exceptions.ConflictException>();
        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    // Burada imzalı kesin initialize reddinin ortak başarısızlık servisiyle rezervasyonu serbest bıraktığını doğruluyorum.
    [Fact]
    public async Task InitializeForCurrentUserAsync_Should_Cancel_On_Definitive_Signed_Provider_Rejection()
    {
        var (order, variant) = CreateReservedPayableOrder();
        var repository = CreateOrderRepository(order);
        var variants = new Mock<IProductVariantRepository>();
        variants.Setup(item => item.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([variant]);
        var gateway = new RecordingCheckoutFormGateway
        {
            InitializeResultFactory = request => new CheckoutFormInitializeResult(
                false,
                "test-token",
                null,
                null,
                "Provider rejected initialization.",
                true,
                request.ConversationId)
        };
        var service = CreateService(
            repository.Object,
            gateway,
            CreateUnitOfWork().Object,
            variants.Object);

        var result = await service.InitializeForCurrentUserAsync(
            order.Id,
            "iyzico_application_key_failure_02",
            "127.0.0.1",
            CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Failed);
        order.Status.Should().Be(OrderStatus.Cancelled);
        variant.Stock.Should().Be(2);
    }

    // Burada bağlantı hatasının kesin başarısızlık sayılmayıp Pending ödeme ve stok rezervasyonunu koruduğunu doğruluyorum.
    [Fact]
    public async Task InitializeForCurrentUserAsync_Should_Keep_Pending_On_Connectivity_Error()
    {
        var (order, variant) = CreateReservedPayableOrder();
        var repository = CreateOrderRepository(order);
        var gateway = new RecordingCheckoutFormGateway
        {
            InitializeException = new HttpRequestException("provider unavailable")
        };
        var service = CreateService(repository.Object, gateway, CreateUnitOfWork().Object);

        var action = () => service.InitializeForCurrentUserAsync(
            order.Id,
            "iyzico_application_key_unknown_01",
            "127.0.0.1",
            CancellationToken.None);

        await action.Should().ThrowAsync<ECommerce.Application.Common.Exceptions.ConflictException>();
        order.Payments.Should().ContainSingle(payment => payment.Status == PaymentStatus.Pending);
        order.Status.Should().Be(OrderStatus.Pending);
        order.ReservationExpiresAt.Should().NotBeNull();
        variant.Stock.Should().Be(1);
    }

    // Burada test servisini kullanılmayan guest bağımlılıklarını yalıtarak oluşturuyorum.
    private static CheckoutFormPaymentService CreateService(
        IOrderRepository orders,
        ICheckoutFormGateway gateway,
        IUnitOfWork unitOfWork,
        IProductVariantRepository? variants = null,
        IOrderNotificationService? notifications = null,
        ICartRepository? carts = null,
        IDateTimeProvider? clock = null,
        IAuthoritativeSalesMetricService? salesMetrics = null)
    {
        var guestRepository = Mock.Of<IGuestOrderRepository>();
        var guestAccess = new GuestOrderAccessService(
            guestRepository,
            Mock.Of<IEmailOutboxRepository>(),
            Mock.Of<IUserRepository>(),
            Mock.Of<IGuestTokenService>(),
            Mock.Of<IGuestOrderAccessTokenProtector>(),
            Mock.Of<IGuestCheckoutProtectionService>(),
            new FixedClock(),
            unitOfWork);
        var notificationMock = new Mock<IOrderNotificationService>();
        notificationMock.Setup(service => service.QueuePaymentResultAsync(
                It.IsAny<Order>(),
                It.IsAny<Payment>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notificationMock.Setup(service => service.QueueOrderStatusChangedAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var resolvedNotifications = notifications ?? notificationMock.Object;
        var resolvedClock = clock ?? new FixedClock();
        var inventory = new OrderInventoryService(variants ?? Mock.Of<IProductVariantRepository>());
        var coupons = new OrderCouponService(Mock.Of<ICouponRepository>(), resolvedClock);
        return new CheckoutFormPaymentService(
            orders,
            guestRepository,
            guestAccess,
            gateway,
            new StubCurrentUser(7),
            resolvedClock,
            unitOfWork,
            resolvedNotifications,
            carts ?? Mock.Of<ICartRepository>(),
            new DefinitivePaymentFailureService(inventory, coupons, resolvedNotifications, resolvedClock),
            salesMetrics ?? Mock.Of<IAuthoritativeSalesMetricService>());
    }

    // Burada üye ödeme testinde aynı takipli aggregate'ı döndüren repository mockunu hazırlıyorum.
    private static Mock<IOrderRepository> CreateOrderRepository(Order order)
    {
        var repository = new Mock<IOrderRepository>();
        repository.Setup(item => item.GetByIdForUserForUpdateAsync(
                order.Id,
                7,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        repository.Setup(item => item.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        repository.Setup(item => item.GetByPaymentProviderTokenAsync(
                "test-token",
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        repository.Setup(item => item.AddPaymentAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return repository;
    }

    // Burada iki aşamalı initialize ve completion transaction callbacklerini gerçekten çalıştıran unit of work hazırlıyorum.
    private static Mock<IUnitOfWork> CreateUnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.ExecuteInSerializableTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<bool>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<bool>>, CancellationToken>((action, token) => action(token));
        unitOfWork.Setup(unit => unit.ExecuteInSerializableTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<CheckoutFormSessionDto>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<CheckoutFormSessionDto>>, CancellationToken>(
                (action, token) => action(token));
        unitOfWork.Setup(unit => unit.ExecuteInSerializableTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<CheckoutFormCompletionDto>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<CheckoutFormCompletionDto>>, CancellationToken>(
                (action, token) => action(token));
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return unitOfWork;
    }

    // Burada müşteri, adres ve ürün snapshot'ları tamamlanmış ödenebilir sipariş oluşturuyorum.
    private static Order CreatePayableOrder()
    {
        var order = new Order(7, $"ORD-{Guid.NewGuid():N}"[..24], 100m, 0m, 0m, 0m, 100m);
        order.SetCustomerSnapshot("Ada", "Lovelace", "ada@example.com", "+905551112233");
        order.SetGuestShippingAddressSnapshot(
            "Home", "Ada", "Lovelace", "+905551112233", "Istanbul", "Kadikoy", "Mahalle", "Test address", "34000");
        order.SetBillingAddressSnapshot(
            null, "Home", "Ada", "Lovelace", "+905551112233", "Istanbul", "Kadikoy", "Mahalle", "Test address", "34000");
        order.AddItem(1, Guid.NewGuid(), "Test Product", "SKU-001", 100m, 1);
        order.EnsureItemsMatchSubTotal();
        return order;
    }

    // Burada canlı sandbox yuvarlama örneğiyle aynı iki kalemli ve vergili sipariş toplamını hazırlıyorum.
    private static Order CreatePayableOrderWithTwoItems()
    {
        var order = new Order(
            7,
            $"ORD-{Guid.NewGuid():N}"[..24],
            1498.34m,
            0m,
            449.66m,
            0m,
            1948.00m,
            shippingMethodId: Guid.NewGuid(),
            shippingMethodName: "Test Shipping");
        order.SetCustomerSnapshot("Ada", "Lovelace", "ada@example.com", "+905551112233");
        order.SetGuestShippingAddressSnapshot(
            "Home", "Ada", "Lovelace", "+905551112233", "Istanbul", "Kadikoy", "Mahalle", "Test address", "34000");
        order.SetBillingAddressSnapshot(
            null, "Home", "Ada", "Lovelace", "+905551112233", "Istanbul", "Kadikoy", "Mahalle", "Test address", "34000");
        order.AddItem(1, Guid.NewGuid(), "Test Product One", "SKU-001", 749.17m, 1);
        order.AddItem(2, Guid.NewGuid(), "Test Product Two", "SKU-002", 749.17m, 1);
        order.EnsureItemsMatchSubTotal();
        return order;
    }

    // Burada satış hareketi uygulanmış ve tam on beş dakika ayrılmış stok taşıyan ödenebilir sipariş hazırlıyorum.
    private static (Order Order, ProductVariant Variant) CreateReservedPayableOrder()
    {
        var product = new Product("Payment Product", "payment-product", "PAYMENT-MAIN", status: ProductStatus.Active)
            .WithId(41);
        var variant = new ProductVariant(product.Id, "Default", "PAYMENT-SKU", 100m, 2);
        var order = CreatePayableOrderWithVariant(product.Id, variant.Id, product.Title, variant.Sku);
        variant.ApplyStockMovement(-1, StockMovementType.Sale, "Checkout reservation.", order.Id);
        order.StartStockReservation(new FixedClock().UtcNow, TimeSpan.FromMinutes(15));
        return (order, variant);
    }

    // Burada belirli varyant kimliğiyle ödeme testinin güvenilir sipariş snapshot'ını oluşturuyorum.
    private static Order CreatePayableOrderWithVariant(long productId, Guid variantId, string title, string sku)
    {
        var order = new Order(7, $"ORD-{Guid.NewGuid():N}"[..24], 100m, 0m, 0m, 0m, 100m);
        order.SetCustomerSnapshot("Ada", "Lovelace", "ada@example.com", "+905551112233");
        order.SetGuestShippingAddressSnapshot(
            "Home", "Ada", "Lovelace", "+905551112233", "Istanbul", "Kadikoy", "Mahalle", "Test address", "34000");
        order.SetBillingAddressSnapshot(
            null, "Home", "Ada", "Lovelace", "+905551112233", "Istanbul", "Kadikoy", "Mahalle", "Test address", "34000");
        order.AddItem(productId, variantId, title, sku, 100m, 1);
        order.EnsureItemsMatchSubTotal();
        return order;
    }

    // Burada callback testleri için token ve conversation kimliği yerel kayıtla eşleşen Pending ödeme ekliyorum.
    private static Payment AddInitializedPayment(Order order, string idempotencyKey)
    {
        var payment = new Payment(order.Id, PaymentProvider.Iyzico, order.GrandTotal, idempotencyKey);
        order.AddPayment(payment);
        payment.InitializeCheckoutForm(
            "test-token",
            payment.Id.ToString("N"),
            "https://sandbox-api.iyzipay.com/checkoutform/test-token",
            DateTime.UtcNow.AddMinutes(30));
        return payment;
    }

    // Burada provider callback testleri için kimliği doğrulanabilir ödeme sonucunu hazırlıyorum.
    private static CheckoutFormRetrieveResult CreateRetrieveResult(
        Order order,
        Payment payment,
        CheckoutFormPaymentState state,
        int fraudStatus)
    {
        return new CheckoutFormRetrieveResult(
            state,
            "test-token",
            payment.Id.ToString("N"),
            order.Id.ToString("N"),
            "TRY",
            order.SubTotal,
            order.GrandTotal,
            1,
            state switch
            {
                CheckoutFormPaymentState.Paid => "provider-paid-id",
                CheckoutFormPaymentState.Failed => "provider-failure-id",
                _ => null
            },
            fraudStatus,
            state == CheckoutFormPaymentState.Failed ? "Provider rejected payment." : null,
            state == CheckoutFormPaymentState.Paid
                ? CreateProviderItems(order, order.GrandTotal)
                : null);
    }

    // Burada test sipariş kalemlerini provider'ın gerçek item transaction sözleşmesine dönüştürüyorum.
    private static IReadOnlyList<CheckoutFormItemTransaction> CreateProviderItems(
        Order order,
        decimal paidPrice)
    {
        var item = order.Items.Single();
        return
        [
            new CheckoutFormItemTransaction(
                $"provider-item-{item.Id:N}",
                item.Id.ToString("N"),
                item.TotalPrice,
                paidPrice,
                1)
        ];
    }

    private sealed class StubCurrentUser : ICurrentUserService
    {
        public long? UserId { get; }

        // Burada application testinin sabit kullanıcı kimliğini sağlıyorum.
        public StubCurrentUser(long userId)
        {
            UserId = userId;
        }
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        private readonly DateTime _utcNow;

        // Burada testlerin varsayılan veya senaryoya özel UTC zamanını sabitliyorum.
        public FixedClock(DateTime? utcNow = null)
        {
            _utcNow = utcNow ?? new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);
        }

        public DateTime UtcNow => _utcNow;
    }

    private sealed class RecordingCheckoutFormGateway : ICheckoutFormGateway
    {
        public PaymentProvider Provider => PaymentProvider.Iyzico;
        public bool IsEnabled => true;
        public int InitializeCallCount { get; private set; }
        public int RetrieveCallCount { get; private set; }
        public int ReversalCallCount { get; private set; }
        public CheckoutFormInitializeGatewayRequest? LastInitializeRequest { get; private set; }
        public string? LastRetrieveConversationId { get; private set; }
        public CheckoutFormRetrieveResult RetrieveResult { get; init; } = null!;
        public Func<CheckoutFormInitializeGatewayRequest, CheckoutFormInitializeResult>? InitializeResultFactory { get; init; }
        public Exception? InitializeException { get; init; }
        public Exception? RetrieveException { get; init; }
        public LatePaymentReversalResult ReversalResult { get; init; } = new(true, false);

        // Burada form başlatma isteğini kaydedip geçerli sandbox oturumu döndürüyorum.
        public Task<CheckoutFormInitializeResult> InitializeAsync(
            CheckoutFormInitializeGatewayRequest request,
            CancellationToken cancellationToken = default)
        {
            InitializeCallCount++;
            LastInitializeRequest = request;
            if (InitializeException is not null)
            {
                throw InitializeException;
            }

            if (InitializeResultFactory is not null)
            {
                return Task.FromResult(InitializeResultFactory(request));
            }

            return Task.FromResult(new CheckoutFormInitializeResult(
                true,
                "test-token",
                "https://sandbox-api.iyzipay.com/checkoutform/test-token",
                DateTime.UtcNow.AddMinutes(30),
                null,
                false,
                request.ConversationId));
        }

        // Burada callback testinde önceden hazırlanmış sağlayıcı sonucunu döndürüyorum.
        public Task<CheckoutFormRetrieveResult> RetrieveAsync(
            string token,
            string conversationId,
            CancellationToken cancellationToken = default)
        {
            RetrieveCallCount++;
            LastRetrieveConversationId = conversationId;
            if (RetrieveException is not null)
            {
                throw RetrieveException;
            }

            return Task.FromResult(RetrieveResult);
        }

        // Burada terk edilmiş ödeme testinde geç tahsilat ters işlem çağrısını kaydediyorum.
        public Task<LatePaymentReversalResult> ReverseLatePaymentAsync(
            string providerPaymentId,
            string conversationId,
            decimal expectedAmount,
            CancellationToken cancellationToken = default)
        {
            ReversalCallCount++;
            return Task.FromResult(ReversalResult);
        }

        // Burada cancellation testleri dışındaki reporting çağrısını bilinçli olarak desteklenmiyor bırakıyorum.
        public Task<PaymentReversalReport> RetrieveReversalReportAsync(
            string providerPaymentId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        // Burada cancellation testleri dışındaki provider cancel çağrısını bilinçli olarak desteklenmiyor bırakıyorum.
        public Task<PaymentReversalGatewayResult> CancelPaymentAsync(
            string providerPaymentId,
            string conversationId,
            decimal expectedPaidAmount,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        // Burada cancellation testleri dışındaki item refund çağrısını bilinçli olarak desteklenmiyor bırakıyorum.
        public Task<PaymentReversalGatewayResult> RefundPaymentItemAsync(
            string providerPaymentId,
            string providerPaymentTransactionId,
            string conversationId,
            decimal amount,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        // Burada unit test kapsamı dışındaki webhook imzasını geçerli kabul etmiyorum.
        public bool ValidateWebhookSignature(CheckoutFormWebhookNotification notification, string signature)
        {
            return false;
        }
    }
}
