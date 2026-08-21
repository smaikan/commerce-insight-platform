using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Payments;
using ECommerce.Application.Common.Security;
using ECommerce.Application.GuestOrders;
using ECommerce.Application.Payments;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
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
        order.Status.Should().Be(OrderStatus.Confirmed);
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
                "28157797",
                1,
                null)
        };
        var unitOfWork = CreateUnitOfWork();
        var service = CreateService(repository.Object, gateway, unitOfWork.Object);

        var result = await service.CompleteByTokenAsync("test-token", CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Paid);
        payment.Status.Should().Be(PaymentStatus.Paid);
        payment.TransactionId.Should().Be("28157797");
        payment.FraudStatus.Should().Be(1);
        order.Status.Should().Be(OrderStatus.Paid);
        gateway.RetrieveCallCount.Should().Be(1);
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

    // Burada provider tutarı siparişle eşleşmediğinde yerel ödeme ve siparişin değişmediğini doğruluyorum.
    [Fact]
    public async Task CompleteByTokenAsync_Should_Reject_Amount_Mismatch_Without_Mutation()
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
                order.SubTotal,
                order.GrandTotal + 1m,
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

    // Burada test servisini kullanılmayan guest bağımlılıklarını yalıtarak oluşturuyorum.
    private static CheckoutFormPaymentService CreateService(
        IOrderRepository orders,
        ICheckoutFormGateway gateway,
        IUnitOfWork unitOfWork)
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
        var notifications = new Mock<IOrderNotificationService>();
        notifications.Setup(service => service.QueuePaymentResultAsync(
                It.IsAny<Order>(),
                It.IsAny<Payment>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return new CheckoutFormPaymentService(
            orders,
            guestRepository,
            guestAccess,
            gateway,
            new StubCurrentUser(7),
            new FixedClock(),
            unitOfWork,
            notifications.Object,
            Mock.Of<ICartRepository>());
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
        public DateTime UtcNow => new(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class RecordingCheckoutFormGateway : ICheckoutFormGateway
    {
        public PaymentProvider Provider => PaymentProvider.Iyzico;
        public bool IsEnabled => true;
        public int InitializeCallCount { get; private set; }
        public int RetrieveCallCount { get; private set; }
        public CheckoutFormInitializeGatewayRequest? LastInitializeRequest { get; private set; }
        public CheckoutFormRetrieveResult RetrieveResult { get; init; } = null!;

        // Burada form başlatma isteğini kaydedip geçerli sandbox oturumu döndürüyorum.
        public Task<CheckoutFormInitializeResult> InitializeAsync(
            CheckoutFormInitializeGatewayRequest request,
            CancellationToken cancellationToken = default)
        {
            InitializeCallCount++;
            LastInitializeRequest = request;
            return Task.FromResult(new CheckoutFormInitializeResult(
                true,
                "test-token",
                "https://sandbox-api.iyzipay.com/checkoutform/test-token",
                DateTime.UtcNow.AddMinutes(30),
                null));
        }

        // Burada callback testinde önceden hazırlanmış sağlayıcı sonucunu döndürüyorum.
        public Task<CheckoutFormRetrieveResult> RetrieveAsync(
            string token,
            CancellationToken cancellationToken = default)
        {
            RetrieveCallCount++;
            return Task.FromResult(RetrieveResult);
        }

        // Burada unit test kapsamı dışındaki webhook imzasını geçerli kabul etmiyorum.
        public bool ValidateWebhookSignature(CheckoutFormWebhookNotification notification, string signature)
        {
            return false;
        }
    }
}


