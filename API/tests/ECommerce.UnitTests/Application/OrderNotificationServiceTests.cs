using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class OrderNotificationServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 22, 18, 0, 0, DateTimeKind.Utc);

    // Burada yalnız kargoya verildi bildiriminde kargo firması ve takip alanlarının taşındığını doğruluyorum.
    [Fact]
    public async Task QueueOrderStatusChanged_Should_Include_Shipment_Details_Only_When_Shipped()
    {
        var order = CreateShippedOrder();
        EmailOutboxMessage? queuedMessage = null;
        var outbox = CreateOutbox(message => queuedMessage = message);
        var service = CreateService(outbox.Object);

        await service.QueueOrderStatusChangedAsync(order);

        queuedMessage.Should().NotBeNull();
        queuedMessage!.Status.Should().Be(nameof(OrderStatus.Shipped));
        queuedMessage.ShippingCarrier.Should().Be("Yurtiçi Kargo");
        queuedMessage.TrackingNumber.Should().Be("TRACK-123");
        queuedMessage.TrackingUrl.Should().Be("https://track.example.com/TRACK-123");
    }

    // Burada teslim, ücret iadesi ve değişim onayı bildirimlerinin geçmiş kargo alanlarını taşımadığını doğruluyorum.
    [Theory]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Refunded)]
    [InlineData(OrderStatus.ReturnApproved)]
    public async Task QueueOrderStatusChanged_Should_Omit_Shipment_Details_After_Shipped(
        OrderStatus targetStatus)
    {
        var order = CreateShippedOrder();
        MoveToTargetStatus(order, targetStatus);
        EmailOutboxMessage? queuedMessage = null;
        var outbox = CreateOutbox(message => queuedMessage = message);
        var service = CreateService(outbox.Object);

        await service.QueueOrderStatusChangedAsync(order);

        queuedMessage.Should().NotBeNull();
        queuedMessage!.Status.Should().Be(targetStatus.ToString());
        queuedMessage.ShippingCarrier.Should().BeNull();
        queuedMessage.TrackingNumber.Should().BeNull();
        queuedMessage.TrackingUrl.Should().BeNull();
    }

    // Burada provider geri alımı tamamlandığında gerçek tahsilat tutarının ayrı outbox mesajına taşındığını doğruluyorum.
    [Fact]
    public async Task QueuePaymentReversalCompleted_Should_Create_A_Deduplicated_Refund_Message()
    {
        var domainUtcNow = DateTime.UtcNow;
        var order = new Order(null, "ORD-REFUND-NOTIFY", 250m, 0m, 0m, 0m, 250m);
        order.SetCustomerSnapshot("Ada", "Lovelace", "ada@example.com", "+905551112233");
        order.ChangeStatus(OrderStatus.Confirmed, domainUtcNow);
        var payment = new Payment(order.Id, PaymentProvider.Iyzico, 250m, "refund-notification-intent");
        order.AddPayment(payment);
        payment.InitializeCheckoutForm(
            "refund-notification-token",
            payment.Id.ToString("N"),
            "https://sandbox-cpp.iyzipay.com?token=refund-notification-token",
            domainUtcNow.AddMinutes(30));
        payment.MarkAsPaid("refund-notification-provider-payment", 1, 250m, 1);
        order.ChangeStatus(OrderStatus.Paid, domainUtcNow.AddMinutes(1));
        var operation = new OrderCancellationOperation(
            order,
            payment,
            OrderCancellationInitiatorType.Member,
            PaymentReversalType.Cancel,
            domainUtcNow.AddMinutes(2));
        operation.TryClaim(domainUtcNow.AddMinutes(2), TimeSpan.FromMinutes(2)).Should().BeTrue();
        payment.MarkAsCancelledAfterProviderReversal();
        order.ChangeStatus(OrderStatus.Cancelled, domainUtcNow.AddMinutes(3));
        operation.MarkCompleted(PaymentReversalType.Cancel, domainUtcNow.AddMinutes(3));
        EmailOutboxMessage? queuedMessage = null;
        var outbox = CreateOutbox(message => queuedMessage = message);
        var service = CreateService(outbox.Object);

        await service.QueuePaymentReversalCompletedAsync(order, payment, operation);

        queuedMessage.Should().NotBeNull();
        queuedMessage!.Type.Should().Be(EmailOutboxMessageType.PaymentReversalCompleted);
        queuedMessage.DeduplicationKey.Should().Be($"payment-reversal-completed:{operation.Id:N}");
        queuedMessage.Email.Should().Be("ada@example.com");
        queuedMessage.OrderNumber.Should().Be(order.OrderNumber);
        queuedMessage.Amount.Should().Be(250m);
        queuedMessage.Status.Should().Be(nameof(PaymentReversalType.Cancel));
    }

    // Burada bildirim testleri için kargo bilgisi atanmış geçerli bir sipariş yaşam döngüsü oluşturuyorum.
    private static Order CreateShippedOrder()
    {
        var order = new Order(null, "ORD-NOTIFY-1", 0m, 0m, 0m, 0m, 0m);
        order.SetCustomerSnapshot("Ada", "Lovelace", "ada@example.com", "+905551112233");
        order.ChangeStatus(OrderStatus.Confirmed, UtcNow.AddMinutes(1));
        order.ChangeStatus(OrderStatus.Paid, UtcNow.AddMinutes(2));
        order.ChangeStatus(OrderStatus.Preparing, UtcNow.AddMinutes(3));
        order.SetShipment(
            "Yurtiçi Kargo",
            "TRACK-123",
            "https://track.example.com/TRACK-123",
            UtcNow.AddMinutes(4));
        return order;
    }

    // Burada kargo sonrası hedef duruma gerçek domain geçişlerini kullanarak ulaşıyorum.
    private static void MoveToTargetStatus(Order order, OrderStatus targetStatus)
    {
        order.ChangeStatus(OrderStatus.Delivered, UtcNow.AddMinutes(5));
        if (targetStatus == OrderStatus.Delivered)
        {
            return;
        }

        if (targetStatus == OrderStatus.Refunded)
        {
            order.MarkRefunded();
            return;
        }

        order.MarkReturnRequested();
        order.MarkReturnApproved();
    }

    // Burada test edilen mesajı yakalayan başarılı outbox repository taklidini hazırlıyorum.
    private static Mock<IEmailOutboxRepository> CreateOutbox(Action<EmailOutboxMessage> capture)
    {
        var outbox = new Mock<IEmailOutboxRepository>();
        outbox.Setup(repository => repository.AddAsync(
                It.IsAny<EmailOutboxMessage>(),
                It.IsAny<CancellationToken>()))
            .Callback<EmailOutboxMessage, CancellationToken>((message, _) => capture(message))
            .Returns(Task.CompletedTask);
        return outbox;
    }

    // Burada sabit saat ve snapshot alıcısıyla bildirim servisini test için hazırlıyorum.
    private static OrderNotificationService CreateService(IEmailOutboxRepository outbox)
    {
        return new OrderNotificationService(
            Mock.Of<IUserRepository>(),
            outbox,
            Mock.Of<IDateTimeProvider>(clock => clock.UtcNow == UtcNow));
    }
}
