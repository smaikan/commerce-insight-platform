using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Payments;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Orders.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class OrderCancellationServiceTests
{
    // Burada Paid siparişin aynı gün provider cancel başarısından sonra bütün yerel etkilerinin bir kez tamamlandığını doğruluyorum.
    [Fact]
    public async Task RequestAsync_Should_Complete_A_Same_Day_Paid_Cancellation()
    {
        var fixture = CreateFixture(itemCount: 1);

        var result = await fixture.Service.RequestAsync(
            fixture.Order,
            OrderCancellationInitiatorType.Member,
            "/api/orders",
            CancellationToken.None);

        result.IsCompleted.Should().BeTrue();
        result.Order!.Status.Should().Be(OrderStatus.Cancelled);
        fixture.Payment.Status.Should().Be(PaymentStatus.Cancelled);
        fixture.Variants.Should().OnlyContain(variant => variant.Stock == 5);
        fixture.Variants.SelectMany(variant => variant.StockMovements).Should().ContainSingle(movement =>
            movement.Type == StockMovementType.Cancellation && movement.OrderId == fixture.Order.Id);
        fixture.Gateway.CancelCallCount.Should().Be(1);
        fixture.Operations.Stored!.Status.Should().Be(OrderCancellationOperationStatus.Completed);
        fixture.Notifications.Verify(service => service.QueueOrderStatusChangedAsync(
            fixture.Order,
            It.IsAny<CancellationToken>()), Times.Once);
        fixture.Notifications.Verify(service => service.QueuePaymentReversalCompletedAsync(
            fixture.Order,
            fixture.Payment,
            fixture.Operations.Stored,
            It.IsAny<CancellationToken>()), Times.Once);
        fixture.SalesMetrics.Verify(metric => metric.ReverseCancelledOrderAsync(
            fixture.Order,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada cancel isteği timeout verdiğinde sipariş ve ödeme değişmeden 202 polling operasyonunun döndüğünü doğruluyorum.
    [Fact]
    public async Task RequestAsync_Should_Return_A_Reconciliation_Operation_After_Provider_Timeout()
    {
        var fixture = CreateFixture(itemCount: 1);
        fixture.Gateway.CancelThrows = true;

        var result = await fixture.Service.RequestAsync(
            fixture.Order,
            OrderCancellationInitiatorType.Guest,
            "/api/guest-orders",
            CancellationToken.None);

        result.IsCompleted.Should().BeFalse();
        result.Operation.Should().NotBeNull();
        result.Operation!.Status.Should().Be(OrderCancellationOperationStatus.ReconciliationPending);
        result.Operation.PollingUrl.Should().Be($"/api/guest-orders/{fixture.Order.Id}/cancellation");
        fixture.Order.Status.Should().Be(OrderStatus.Paid);
        fixture.Payment.Status.Should().Be(PaymentStatus.Paid);
        fixture.Variants.Should().OnlyContain(variant => variant.Stock == 4);
        fixture.Notifications.Verify(service => service.QueueOrderStatusChangedAsync(
            It.IsAny<Order>(),
            It.IsAny<CancellationToken>()), Times.Never);
        fixture.Notifications.Verify(service => service.QueuePaymentReversalCompletedAsync(
            It.IsAny<Order>(),
            It.IsAny<Payment>(),
            It.IsAny<OrderCancellationOperation>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada çok ürünlü standart refund'ın her gerçek provider item tutarını ayrı çağırıp toplam tahsilatı eksiksiz geri aldığını doğruluyorum.
    [Fact]
    public async Task ProcessAsync_Should_Refund_Each_Provider_Item_And_Complete_Locally()
    {
        var fixture = CreateFixture(itemCount: 2);
        var operation = new OrderCancellationOperation(
            fixture.Order,
            fixture.Payment,
            OrderCancellationInitiatorType.Member,
            PaymentReversalType.Refund,
            fixture.Clock.UtcNow);
        await fixture.Operations.AddAsync(operation);

        var completed = await fixture.Service.ProcessAsync(operation.Id);

        completed.Should().BeTrue();
        fixture.Gateway.RefundAmounts.Should().BeEquivalentTo([40m, 60m]);
        fixture.Gateway.CancelCallCount.Should().Be(0);
        fixture.Payment.Status.Should().Be(PaymentStatus.Refunded);
        fixture.Order.Status.Should().Be(OrderStatus.Cancelled);
        operation.Items.Should().OnlyContain(item => item.Status == PaymentReversalItemStatus.Completed);
        fixture.Variants.Should().OnlyContain(variant => variant.Stock == 5);
        fixture.Notifications.Verify(service => service.QueuePaymentReversalCompletedAsync(
            fixture.Order,
            fixture.Payment,
            operation,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada reporting kimlik/tutar uyuşmazlığının provider mutasyonu ve yerel stok iadesi yapmadan manual review'a alındığını doğruluyorum.
    [Fact]
    public async Task ProcessAsync_Should_Not_Mutate_When_Reporting_Does_Not_Match_The_Order()
    {
        var fixture = CreateFixture(itemCount: 1);
        var operation = new OrderCancellationOperation(
            fixture.Order,
            fixture.Payment,
            OrderCancellationInitiatorType.Member,
            PaymentReversalType.Cancel,
            fixture.Clock.UtcNow);
        await fixture.Operations.AddAsync(operation);
        fixture.Gateway.Report = fixture.Gateway.Report with { Currency = "USD" };

        var completed = await fixture.Service.ProcessAsync(operation.Id);

        completed.Should().BeFalse();
        operation.Status.Should().Be(OrderCancellationOperationStatus.ManualReview);
        fixture.Gateway.CancelCallCount.Should().Be(0);
        fixture.Gateway.RefundAmounts.Should().BeEmpty();
        fixture.Payment.Status.Should().Be(PaymentStatus.Paid);
        fixture.Order.Status.Should().Be(OrderStatus.Paid);
        fixture.Variants.Should().OnlyContain(variant => variant.Stock == 4);
    }

    // Burada reporting'deki başarılı cancel kanıtının provider'a ikinci cancel göndermeden yerel etkileri tamamladığını doğruluyorum.
    [Fact]
    public async Task ProcessAsync_Should_Complete_From_Successful_Cancel_Reporting_Evidence()
    {
        var fixture = CreateFixture(itemCount: 1);
        var operation = new OrderCancellationOperation(
            fixture.Order,
            fixture.Payment,
            OrderCancellationInitiatorType.Member,
            PaymentReversalType.Cancel,
            fixture.Clock.UtcNow);
        await fixture.Operations.AddAsync(operation);
        fixture.Gateway.Report = fixture.Gateway.Report with
        {
            Cancels =
            [
                new PaymentReversalReportCancel(
                    operation.ProviderConversationId,
                    fixture.Payment.ProviderPaidAmount!.Value,
                    1,
                    "TRY")
            ]
        };

        var completed = await fixture.Service.ProcessAsync(operation.Id);

        completed.Should().BeTrue();
        fixture.Gateway.CancelCallCount.Should().Be(0);
        fixture.Order.Status.Should().Be(OrderStatus.Cancelled);
        fixture.Payment.Status.Should().Be(PaymentStatus.Cancelled);
    }

    // Burada başarısız reporting cancel kaydının finansal başarı sayılmayıp gerçek cancel çağrısının sürdüğünü doğruluyorum.
    [Fact]
    public async Task ProcessAsync_Should_Ignore_Unsuccessful_Cancel_Reporting_Evidence()
    {
        var fixture = CreateFixture(itemCount: 1);
        var operation = new OrderCancellationOperation(
            fixture.Order,
            fixture.Payment,
            OrderCancellationInitiatorType.Member,
            PaymentReversalType.Cancel,
            fixture.Clock.UtcNow);
        await fixture.Operations.AddAsync(operation);
        fixture.Gateway.Report = fixture.Gateway.Report with
        {
            Cancels =
            [
                new PaymentReversalReportCancel(
                    operation.ProviderConversationId,
                    fixture.Payment.ProviderPaidAmount!.Value,
                    0,
                    "TRY")
            ]
        };

        var completed = await fixture.Service.ProcessAsync(operation.Id);

        completed.Should().BeTrue();
        fixture.Gateway.CancelCallCount.Should().Be(1);
    }

    // Burada manual review'a düşmüş siparişe tekrarlanan isteğin ikinci operasyon veya provider mutasyonu üretmediğini doğruluyorum.
    [Fact]
    public async Task RequestAsync_Should_Reuse_A_ManualReview_Operation_Without_A_Second_Reversal()
    {
        var fixture = CreateFixture(itemCount: 1);
        var operation = new OrderCancellationOperation(
            fixture.Order,
            fixture.Payment,
            OrderCancellationInitiatorType.Member,
            PaymentReversalType.Cancel,
            fixture.Clock.UtcNow);
        await fixture.Operations.AddAsync(operation);
        fixture.Gateway.Report = fixture.Gateway.Report with { Currency = "USD" };
        await fixture.Service.ProcessAsync(operation.Id);

        var action = () => fixture.Service.RequestAsync(
            fixture.Order,
            OrderCancellationInitiatorType.Member,
            "/api/orders",
            CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ApiContractException>();
        exception.Which.ErrorCode.Should().Be("payment_reversal_manual_review");
        fixture.Operations.Stored.Should().BeSameAs(operation);
        fixture.Operations.AddCount.Should().Be(1);
        fixture.Gateway.CancelCallCount.Should().Be(0);
        fixture.Gateway.RefundAmounts.Should().BeEmpty();
    }

    // Burada eski reporting adapter hatasıyla manual-review'a düşen operasyonun düzeltilmiş raporla aynı intent üzerinden tamamlandığını doğruluyorum.
    [Fact]
    public async Task RequestAsync_Should_Reconcile_A_Technical_ManualReview_Operation()
    {
        var fixture = CreateFixture(itemCount: 1);
        var validReport = fixture.Gateway.Report;
        var operation = new OrderCancellationOperation(
            fixture.Order,
            fixture.Payment,
            OrderCancellationInitiatorType.Member,
            PaymentReversalType.Cancel,
            fixture.Clock.UtcNow);
        await fixture.Operations.AddAsync(operation);
        fixture.Gateway.Report = validReport with { Currency = "USD" };
        await fixture.Service.ProcessAsync(operation.Id);
        operation.Status.Should().Be(OrderCancellationOperationStatus.ManualReview);
        fixture.Gateway.Report = validReport;

        var result = await fixture.Service.RequestAsync(
            fixture.Order,
            OrderCancellationInitiatorType.Member,
            "/api/orders",
            CancellationToken.None);

        result.IsCompleted.Should().BeTrue();
        fixture.Order.Status.Should().Be(OrderStatus.Cancelled);
        fixture.Payment.Status.Should().Be(PaymentStatus.Cancelled);
        fixture.Gateway.CancelCallCount.Should().Be(1);
        fixture.Operations.AddCount.Should().Be(1);
    }

    // Burada servis testlerinin gerçek domain aggregate'ları ve kayıt yapan provider doubles ile çalışan bağımlılıklarını hazırlıyorum.
    private static CancellationFixture CreateFixture(int itemCount)
    {
        var clock = new FixedClock(DateTime.UtcNow);
        var product = new Product(
            "Cancellation Service Product",
            $"cancellation-service-{Guid.NewGuid():N}",
            $"CAN-SERVICE-{Guid.NewGuid():N}"[..30],
            status: ProductStatus.Active)
            .WithId(902);
        var variants = Enumerable.Range(0, itemCount)
            .Select(index => new ProductVariant(
                product.Id,
                $"Variant {index + 1}",
                $"CAN-SVC-{index}-{Guid.NewGuid():N}"[..30],
                index == 0 ? 40m : 60m,
                5))
            .ToList();
        var subTotal = variants.Sum(variant => variant.Price);
        var order = new Order(7, $"ORD-{Guid.NewGuid():N}"[..24], subTotal, 0m, 0m, 0m, subTotal);
        foreach (var variant in variants)
        {
            order.AddItem(product.Id, variant.Id, product.Title, variant.Sku, variant.Price, 1);
            variant.ApplyStockMovement(-1, StockMovementType.Sale, "Checkout reservation.", order.Id);
        }

        order.EnsureItemsMatchSubTotal();
        order.ChangeStatus(OrderStatus.Confirmed, clock.UtcNow);
        var payment = new Payment(order.Id, PaymentProvider.Iyzico, subTotal, $"cancel_service_{Guid.NewGuid():N}");
        order.AddPayment(payment);
        payment.InitializeCheckoutForm(
            $"service-token-{Guid.NewGuid():N}",
            payment.Id.ToString("N"),
            $"https://sandbox-cpp.iyzipay.com?token={Guid.NewGuid():N}",
            DateTime.UtcNow.AddMinutes(30));
        payment.MarkAsPaid($"service-provider-payment-{Guid.NewGuid():N}", 1, subTotal, 1);
        payment.RecordProviderItemTransactions(
            order.Items.OrderBy(item => item.Id).Select(item => new ProviderPaymentItemSnapshot(
                item.Id,
                $"service-provider-item-{item.Id:N}",
                item.TotalPrice,
                item.TotalPrice)).ToList(),
            clock.UtcNow);
        order.ChangeStatus(OrderStatus.Paid, clock.UtcNow);

        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        orders.Setup(repository => repository.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var variantRepository = new Mock<IProductVariantRepository>();
        variantRepository.Setup(repository => repository.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(variants);
        var operations = new InMemoryCancellationOperationRepository();
        var gateway = new RecordingCheckoutFormGateway
        {
            Report = new PaymentReversalReport(
                payment.TransactionId!,
                payment.ProviderConversationId!,
                "TRY",
                order.SubTotal,
                payment.ProviderPaidAmount!.Value,
                "NOT_REFUNDED",
                [],
                payment.ItemTransactions.Select(item => new PaymentReversalReportItem(
                    item.ProviderTransactionId,
                    item.Price,
                    item.PaidPrice,
                    [])).ToList())
        };
        var notifications = new Mock<IOrderNotificationService>();
        var salesMetrics = new Mock<IAuthoritativeSalesMetricService>();
        var service = new OrderCancellationService(
            orders.Object,
            operations,
            Mock.Of<IPendingPaymentCancellationReconciler>(),
            gateway,
            new OrderInventoryService(variantRepository.Object),
            new OrderCouponService(Mock.Of<ICouponRepository>(), clock),
            notifications.Object,
            clock,
            new ImmediateUnitOfWork(),
            salesMetrics.Object);
        return new CancellationFixture(
            service,
            order,
            payment,
            variants,
            operations,
            gateway,
            notifications,
            salesMetrics,
            clock);
    }

    private sealed record CancellationFixture(
        OrderCancellationService Service,
        Order Order,
        Payment Payment,
        IReadOnlyList<ProductVariant> Variants,
        InMemoryCancellationOperationRepository Operations,
        RecordingCheckoutFormGateway Gateway,
        Mock<IOrderNotificationService> Notifications,
        Mock<IAuthoritativeSalesMetricService> SalesMetrics,
        FixedClock Clock);

    private sealed class FixedClock : IDateTimeProvider
    {
        // Burada cancellation testlerindeki bütün zaman kararlarını tek UTC ana bağlamak için saati hazırlıyorum.
        public FixedClock(DateTime utcNow)
        {
            UtcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        }

        public DateTime UtcNow { get; }
    }

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        // Burada in-memory aggregate değişikliklerini kaydedilmiş kabul edip çağrı sayısını izliyorum.
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        // Burada unit test transaction delegesini aynı cancellation tokenıyla doğrudan çalıştırıyorum.
        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            return operation(cancellationToken);
        }
    }

    private sealed class InMemoryCancellationOperationRepository : IOrderCancellationOperationRepository
    {
        public OrderCancellationOperation? Stored { get; private set; }
        public int AddCount { get; private set; }

        // Burada tek test operasyonunu ikinci kayıt üretmeden in-memory depoya ekliyorum.
        public Task AddAsync(OrderCancellationOperation operation, CancellationToken cancellationToken = default)
        {
            AddCount++;
            Stored = operation;
            return Task.CompletedTask;
        }

        // Burada yalnız eşleşen siparişin in-memory cancellation operasyonunu döndürüyorum.
        public Task<OrderCancellationOperation?> GetByOrderIdAsync(
            Guid orderId,
            bool forUpdate,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Stored?.OrderId == orderId ? Stored : null);
        }

        // Burada yalnız eşleşen kimliğin in-memory cancellation operasyonunu döndürüyorum.
        public Task<OrderCancellationOperation?> GetByIdAsync(
            Guid operationId,
            bool forUpdate,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Stored?.Id == operationId ? Stored : null);
        }

        // Burada bounded worker testleri için zamanı gelen tek operasyon kimliğini döndürüyorum.
        public Task<IReadOnlyList<Guid>> GetDueIdsAsync(
            DateTime utcNow,
            int maxCount,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Guid> ids = Stored is not null && maxCount > 0 ? [Stored.Id] : [];
            return Task.FromResult(ids);
        }
    }

    private sealed class RecordingCheckoutFormGateway : ICheckoutFormGateway
    {
        public PaymentProvider Provider => PaymentProvider.Iyzico;
        public bool IsEnabled => true;
        public bool CancelThrows { get; set; }
        public int CancelCallCount { get; private set; }
        public List<decimal> RefundAmounts { get; } = [];
        public PaymentReversalReport Report { get; set; } = null!;

        // Burada bu test double'ında kullanılmayan hosted form başlangıcını açıkça desteklenmez bırakıyorum.
        public Task<CheckoutFormInitializeResult> InitializeAsync(
            CheckoutFormInitializeGatewayRequest request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        // Burada bu test double'ında kullanılmayan CheckoutForm retrieve çağrısını açıkça desteklenmez bırakıyorum.
        public Task<CheckoutFormRetrieveResult> RetrieveAsync(
            string token,
            string conversationId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        // Burada bu test double'ında kullanılmayan abandoned ödeme ters işlemini açıkça desteklenmez bırakıyorum.
        public Task<LatePaymentReversalResult> ReverseLatePaymentAsync(
            string providerPaymentId,
            string conversationId,
            decimal expectedAmount,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        // Burada cancellation servisine hazırlanmış authoritative reporting snapshot'ını döndürüyorum.
        public Task<PaymentReversalReport> RetrieveReversalReportAsync(
            string providerPaymentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Report);
        }

        // Burada aynı gün cancel çağrısını kaydedip istenen test senaryosuna göre başarı veya timeout üretiyorum.
        public Task<PaymentReversalGatewayResult> CancelPaymentAsync(
            string providerPaymentId,
            string conversationId,
            decimal expectedPaidAmount,
            CancellationToken cancellationToken = default)
        {
            CancelCallCount++;
            if (CancelThrows)
            {
                throw new HttpRequestException("Provider timeout.");
            }

            return Task.FromResult(new PaymentReversalGatewayResult(true, false));
        }

        // Burada her standart item refund tutarını kaydedip doğrulanmış başarı döndürüyorum.
        public Task<PaymentReversalGatewayResult> RefundPaymentItemAsync(
            string providerPaymentId,
            string providerPaymentTransactionId,
            string conversationId,
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            RefundAmounts.Add(amount);
            return Task.FromResult(new PaymentReversalGatewayResult(true, false));
        }

        // Burada cancellation servisinin kullanmadığı webhook imza doğrulamasını kapalı tutuyorum.
        public bool ValidateWebhookSignature(CheckoutFormWebhookNotification notification, string signature) => false;
    }
}
