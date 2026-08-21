using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Orders.Services;

// Burada sipariş alanı olaylarını kullanıcı bilgisiyle birleştirip kalıcı e-posta outbox kayıtlarına dönüştürüyorum.
public sealed class OrderNotificationService : IOrderNotificationService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailOutboxRepository _emailOutboxRepository;
    private readonly IDateTimeProvider _clock;

    // Burada bildirim üretmek için kullanıcı, outbox ve saat bağımlılıklarını hazırlıyorum.
    public OrderNotificationService(
        IUserRepository userRepository,
        IEmailOutboxRepository emailOutboxRepository,
        IDateTimeProvider clock)
    {
        _userRepository = userRepository;
        _emailOutboxRepository = emailOutboxRepository;
        _clock = clock;
    }

    // Burada sipariş oluşturma bilgisini kullanıcının e-posta snapshot'ıyla atomik outbox mesajına ekliyorum.
    public async Task QueueOrderCreatedAsync(Order order, CancellationToken cancellationToken = default)
    {
        var recipient = await GetRecipientAsync(order, cancellationToken);
        await _emailOutboxRepository.AddAsync(
            EmailOutboxMessage.CreateOrderCreated(
                recipient.Email,
                recipient.FullName,
                order.Id,
                order.OrderNumber,
                order.GrandTotal,
                _clock.UtcNow),
            cancellationToken);
    }

    // Burada yalnız başarılı veya başarısız kesinleşmiş ödeme sonucunu müşteriye bildirilecek mesaj olarak kuyruğa ekliyorum.
    public async Task QueuePaymentResultAsync(
        Order order,
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        var recipient = await GetRecipientAsync(order, cancellationToken);
        var message = payment.Status switch
        {
            PaymentStatus.Paid => EmailOutboxMessage.CreatePaymentPaid(
                recipient.Email,
                recipient.FullName,
                payment.Id,
                order.OrderNumber,
                payment.Amount,
                _clock.UtcNow),
            PaymentStatus.Failed => EmailOutboxMessage.CreatePaymentFailed(
                recipient.Email,
                recipient.FullName,
                payment.Id,
                order.OrderNumber,
                payment.Amount,
                _clock.UtcNow),
            _ => throw new InvalidOperationException("Only a completed payment result can be notified.")
        };

        await _emailOutboxRepository.AddAsync(message, cancellationToken);
    }

    // Burada kesinleşmiş sipariş durumunu müşteri için tekilleştirilmiş durum bildirimi olarak kuyruğa ekliyorum.
    public async Task QueueOrderStatusChangedAsync(Order order, CancellationToken cancellationToken = default)
    {
        var recipient = await GetRecipientAsync(order, cancellationToken);
        await _emailOutboxRepository.AddAsync(
            EmailOutboxMessage.CreateOrderStatusChanged(
                recipient.Email,
                recipient.FullName,
                order.Id,
                order.OrderNumber,
                order.Status,
                _clock.UtcNow,
                order.ShippingCarrier,
                order.TrackingNumber,
                order.TrackingUrl),
            cancellationToken);
    }

    // Burada yeni iade veya değişim talebini sahibinin e-posta snapshot'ıyla atomik outbox mesajına ekliyorum.
    public async Task QueueReturnRequestedAsync(
        ReturnRequest returnRequest,
        Order order,
        CancellationToken cancellationToken = default)
    {
        EnsureReturnBelongsToOrder(returnRequest, order);
        var recipient = await GetRecipientAsync(order, cancellationToken);
        await _emailOutboxRepository.AddAsync(
            EmailOutboxMessage.CreateReturnRequested(
                recipient.Email,
                recipient.FullName,
                returnRequest.Id,
                order.OrderNumber,
                returnRequest.ReturnNumber,
                _clock.UtcNow),
            cancellationToken);
    }

    // Burada iade veya değişim talebinin kesinleşen durumunu müşteriye tekilleştirilmiş outbox mesajı olarak ekliyorum.
    public async Task QueueReturnStatusChangedAsync(
        ReturnRequest returnRequest,
        Order order,
        CancellationToken cancellationToken = default)
    {
        EnsureReturnBelongsToOrder(returnRequest, order);
        var recipient = await GetRecipientAsync(order, cancellationToken);
        await _emailOutboxRepository.AddAsync(
            EmailOutboxMessage.CreateReturnStatusChanged(
                recipient.Email,
                recipient.FullName,
                returnRequest.Id,
                order.OrderNumber,
                returnRequest.ReturnNumber,
                returnRequest.Status.ToString(),
                _clock.UtcNow),
            cancellationToken);
    }

    // Burada sipariş sahibinin bildirim alacağı geçerli kullanıcı kaydını güvenilir depodan çözümlüyorum.
    private async Task<NotificationRecipient> GetRecipientAsync(Order order, CancellationToken cancellationToken)
    {
        if (order.CustomerSnapshot is not null)
        {
            return new NotificationRecipient(
                order.CustomerSnapshot.Email,
                $"{order.CustomerSnapshot.FirstName} {order.CustomerSnapshot.LastName}");
        }

        if (!order.UserId.HasValue)
        {
            throw new InvalidOperationException("Guest order notification snapshot was not found.");
        }

        var user = await _userRepository.GetByIdAsync(order.UserId.Value, cancellationToken)
            ?? throw new InvalidOperationException("Order notification recipient was not found.");
        return new NotificationRecipient(user.Email, user.FullName);
    }

    // Burada iade bildiriminin başka bir sipariş snapshot'ıyla eşleşmesini engelliyorum.
    private static void EnsureReturnBelongsToOrder(ReturnRequest returnRequest, Order order)
    {
        if (returnRequest is null || order is null || returnRequest.OrderId != order.Id)
        {
            throw new InvalidOperationException("Return notification order does not match the return request.");
        }
    }

    // Burada outbox için gerekli en küçük alıcı snapshot'ını taşıyorum.
    private sealed record NotificationRecipient(string Email, string FullName);
}
