using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Payments;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Payments;

public sealed class CheckoutFormPaymentService
{
    private readonly IOrderRepository _orders;
    private readonly IGuestOrderRepository _guestOrders;
    private readonly GuestOrders.GuestOrderAccessService _guestAccess;
    private readonly ICheckoutFormGateway _gateway;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderNotificationService _notifications;

    // Burada üye ve guest hosted ödeme akışının ortak bağımlılıklarını hazırlıyorum.
    public CheckoutFormPaymentService(
        IOrderRepository orders,
        IGuestOrderRepository guestOrders,
        GuestOrders.GuestOrderAccessService guestAccess,
        ICheckoutFormGateway gateway,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork,
        IOrderNotificationService notifications)
    {
        _orders = orders;
        _guestOrders = guestOrders;
        _guestAccess = guestAccess;
        _gateway = gateway;
        _currentUser = currentUser;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _notifications = notifications;
    }

    // Burada oturumdaki kullanıcının kendi siparişi için idempotent CheckoutForm oturumu başlatıyorum.
    public Task<CheckoutFormSessionDto> InitializeForCurrentUserAsync(
        Guid orderId,
        string idempotencyKey,
        string clientIpAddress,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        return InitializeAsync(
            (token) => _orders.GetByIdForUserForUpdateAsync(orderId, userId, token),
            orderId,
            idempotencyKey,
            clientIpAddress,
            cancellationToken);
    }

    // Burada doğrulanmış guest session'ın kendi siparişi için idempotent CheckoutForm oturumu başlatıyorum.
    public async Task<CheckoutFormSessionDto> InitializeForGuestAsync(
        string sessionToken,
        string csrfToken,
        Guid orderId,
        string idempotencyKey,
        string clientIpAddress,
        CancellationToken cancellationToken)
    {
        Guid? sessionId = null;
        return await InitializeAsync(
            async token =>
            {
                if (!sessionId.HasValue)
                {
                    var session = await _guestAccess.ValidateSessionAsync(
                        sessionToken,
                        csrfToken,
                        true,
                        token);
                    sessionId = session.Id;
                }

                return await _guestOrders.GetOrderForSessionAsync(
                    sessionId.Value,
                    orderId,
                    true,
                    token);
            },
            orderId,
            idempotencyKey,
            clientIpAddress,
            cancellationToken);
    }

    // Burada callback tokenını sağlayıcıdan sorgulayıp kesin sonucu yerel siparişe atomik uygularım.
    public async Task<CheckoutFormCompletionDto> CompleteByTokenAsync(
        string token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > Payment.MaximumProviderTokenLength)
        {
            throw new NotFoundException("Payment attempt was not found.");
        }

        var normalizedToken = token.Trim();
        var snapshot = await _orders.GetByPaymentProviderTokenAsync(
            normalizedToken,
            false,
            cancellationToken)
            ?? throw new NotFoundException("Payment attempt was not found.");
        var snapshotPayment = snapshot.Payments.Single(payment => payment.ProviderToken == normalizedToken);
        if (snapshotPayment.Status != PaymentStatus.Pending)
        {
            return ToCompletion(snapshotPayment);
        }

        var result = await _gateway.RetrieveAsync(normalizedToken, cancellationToken);
        EnsureRetrieveMatches(snapshot, snapshotPayment, result);

        return await _unitOfWork.ExecuteInSerializableTransactionAsync(
            async transactionCancellationToken =>
            {
                var order = await _orders.GetByPaymentProviderTokenAsync(
                    normalizedToken,
                    true,
                    transactionCancellationToken)
                    ?? throw new NotFoundException("Payment attempt was not found.");
                var payment = order.Payments.Single(candidate => candidate.ProviderToken == normalizedToken);
                if (payment.Status != PaymentStatus.Pending)
                {
                    return ToCompletion(payment);
                }

                ApplyProviderResult(order, payment, result);
                if (payment.Status is PaymentStatus.Paid or PaymentStatus.Failed)
                {
                    await _notifications.QueuePaymentResultAsync(order, payment, transactionCancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
                return ToCompletion(payment);
            },
            cancellationToken);
    }

    // Burada imzası doğrulanan iyzico webhook'unu aynı idempotent retrieve akışına yönlendiriyorum.
    public Task<CheckoutFormCompletionDto> CompleteWebhookAsync(
        CheckoutFormWebhookNotification notification,
        string signature,
        CancellationToken cancellationToken)
    {
        if (!_gateway.ValidateWebhookSignature(notification, signature))
        {
            throw new UnauthorizedException("Payment webhook signature is invalid.");
        }

        if (!string.Equals(notification.EventType, "CHECKOUT_FORM_AUTH", StringComparison.Ordinal))
        {
            throw new ConflictException("Payment webhook event type is not supported.");
        }

        return CompleteByTokenAsync(notification.Token, cancellationToken);
    }

    // Burada yerel Pending kaydı önce commit edip provider oturumunu transaction dışında oluşturuyorum.
    private async Task<CheckoutFormSessionDto> InitializeAsync(
        Func<CancellationToken, Task<Order?>> loadOrderForUpdate,
        Guid orderId,
        string idempotencyKey,
        string clientIpAddress,
        CancellationToken cancellationToken)
    {
        if (!_gateway.IsEnabled)
        {
            throw new ConflictException("iyzico CheckoutForm is not configured.");
        }

        var normalizedKey = Payment.NormalizeIdempotencyKey(idempotencyKey);
        Payment? existingPayment = null;
        Payment? createdPayment = null;
        Order? orderSnapshot = null;
        var created = await _unitOfWork.ExecuteInSerializableTransactionAsync(
            async token =>
            {
                var order = await loadOrderForUpdate(token)
                    ?? throw new NotFoundException("Order was not found.");
                var matching = order.Payments.SingleOrDefault(payment => payment.IdempotencyKey == normalizedKey);
                if (matching is not null)
                {
                    if (matching.Provider != PaymentProvider.Iyzico)
                    {
                        throw new ConflictException("The idempotency key belongs to another payment provider.");
                    }

                    if (matching.Status == PaymentStatus.Pending && matching.ProviderToken is null)
                    {
                        createdPayment = matching;
                        orderSnapshot = order;
                        return true;
                    }

                    existingPayment = matching;
                    return false;
                }

                EnsurePaymentCanStart(order);
                if (order.Status == OrderStatus.Pending)
                {
                    order.ChangeStatus(OrderStatus.Confirmed, _clock.UtcNow);
                }

                var payment = new Payment(order.Id, PaymentProvider.Iyzico, order.GrandTotal, normalizedKey);
                order.AddPayment(payment);
                await _orders.AddPaymentAsync(payment, token);
                await _unitOfWork.SaveChangesAsync(token);
                createdPayment = payment;
                orderSnapshot = order;
                return true;
            },
            cancellationToken);

        if (!created)
        {
            var existing = existingPayment ?? throw new ConflictException("Payment attempt could not be resolved.");
            return ToSession(existing);
        }

        var paymentToInitialize = createdPayment!;
        CheckoutFormInitializeResult providerResult;
        try
        {
            providerResult = await _gateway.InitializeAsync(
                BuildGatewayRequest(orderSnapshot!, paymentToInitialize, clientIpAddress),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            providerResult = new CheckoutFormInitializeResult(
                false,
                null,
                null,
                null,
                "Payment provider communication failed.");
        }

        return await SaveInitializeResultAsync(
            orderId,
            paymentToInitialize.Id,
            providerResult,
            cancellationToken);
    }

    // Burada provider initialize sonucunu yalnız oluşturulan bekleyen denemeye uygularım.
    private Task<CheckoutFormSessionDto> SaveInitializeResultAsync(
        Guid orderId,
        Guid paymentId,
        CheckoutFormInitializeResult result,
        CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            async token =>
            {
                var order = await _orders.GetByIdForUpdateAsync(orderId, token)
                    ?? throw new ConflictException("Payment attempt requires reconciliation.");
                var payment = order.Payments.SingleOrDefault(candidate => candidate.Id == paymentId)
                    ?? throw new ConflictException("Payment attempt was not found.");
                if (payment.Status != PaymentStatus.Pending || payment.ProviderToken is not null)
                {
                    return ToSession(payment);
                }

                if (result.Succeeded && result.Token is not null && result.PaymentPageUrl is not null &&
                    result.ExpiresAt.HasValue)
                {
                    payment.InitializeCheckoutForm(
                        result.Token,
                        payment.Id.ToString("N"),
                        result.PaymentPageUrl,
                        result.ExpiresAt.Value);
                }
                else
                {
                    payment.MarkAsFailed(result.FailureReason ?? "Payment form could not be initialized.");
                    await _notifications.QueuePaymentResultAsync(order, payment, token);
                }

                await _unitOfWork.SaveChangesAsync(token);
                return ToSession(payment);
            },
            cancellationToken);
    }

    // Burada sağlayıcı isteğini yalnız siparişte dondurulmuş müşteri, adres ve ürün verilerinden oluşturuyorum.
    private static CheckoutFormInitializeGatewayRequest BuildGatewayRequest(
        Order order,
        Payment payment,
        string clientIpAddress)
    {
        var customer = order.CustomerSnapshot
            ?? throw new ConflictException("Order customer snapshot is required for payment.");
        var billing = order.BillingAddressSnapshot ?? order.ShippingAddressSnapshot
            ?? throw new ConflictException("Order billing address is required for payment.");
        var shipping = order.ShippingAddressSnapshot ?? billing;
        var conversationId = payment.Id.ToString("N");
        return new CheckoutFormInitializeGatewayRequest(
            payment.Id,
            order.Id,
            conversationId,
            order.Id.ToString("N"),
            order.SubTotal,
            order.GrandTotal,
            clientIpAddress,
            new CheckoutFormBuyer(
                order.UserId?.ToString() ?? $"guest-{order.Id:N}",
                customer.FirstName,
                customer.LastName,
                customer.Email,
                customer.PhoneNumber),
            ToAddress(billing),
            ToAddress(shipping),
            order.Items.OrderBy(item => item.Id)
                .Select(item => new CheckoutFormBasketItem(
                    item.Id.ToString("N"),
                    item.ProductTitleSnapshot,
                    item.TotalPrice))
                .ToList());
    }

    // Burada adres snapshot'ını provider bağımsız hosted form adresine dönüştürüyorum.
    private static CheckoutFormAddress ToAddress(OrderAddressSnapshot address)
    {
        return new CheckoutFormAddress(
            $"{address.FirstName} {address.LastName}".Trim(),
            address.City,
            address.District,
            address.FullAddress,
            address.PostalCode);
    }

    // Burada yeni ödeme denemesinin sipariş ve açık deneme kurallarını tek yerde doğruluyorum.
    private static void EnsurePaymentCanStart(Order order)
    {
        if (order.GrandTotal <= 0m)
        {
            throw new ConflictException("This order does not require a payment.");
        }

        if (order.Status is not OrderStatus.Pending and not OrderStatus.Confirmed)
        {
            throw new ConflictException("This order cannot accept another payment attempt.");
        }

        if (order.Payments.Any(payment => payment.Status == PaymentStatus.Pending))
        {
            throw new ConflictException("Another payment attempt is still being processed.");
        }
    }

    // Burada imzalı retrieve sonucunun yerel ödeme değişmezleriyle birebir eşleşmesini zorunlu tutuyorum.
    private static void EnsureRetrieveMatches(
        Order order,
        Payment payment,
        CheckoutFormRetrieveResult result)
    {
        if (!string.Equals(result.Token, payment.ProviderToken, StringComparison.Ordinal) ||
            !string.Equals(result.ConversationId, payment.ProviderConversationId, StringComparison.Ordinal) ||
            !string.Equals(result.BasketId, order.Id.ToString("N"), StringComparison.Ordinal) ||
            !string.Equals(result.Currency, "TRY", StringComparison.Ordinal) ||
            result.Price != order.SubTotal ||
            result.PaidPrice != payment.Amount)
        {
            throw new ConflictException("Payment provider result does not match the order.");
        }
    }

    // Burada doğrulanmış provider durumunu domain ödeme ve sipariş geçişlerine uygularım.
    private void ApplyProviderResult(Order order, Payment payment, CheckoutFormRetrieveResult result)
    {
        if (result.State == CheckoutFormPaymentState.Paid && result.FraudStatus == 1 &&
            !string.IsNullOrWhiteSpace(result.ProviderPaymentId))
        {
            payment.MarkAsPaid(result.ProviderPaymentId, result.FraudStatus);
            if (order.Status == OrderStatus.Pending)
            {
                order.ChangeStatus(OrderStatus.Confirmed, _clock.UtcNow);
            }

            if (order.Status == OrderStatus.Confirmed)
            {
                order.ChangeStatus(OrderStatus.Paid, _clock.UtcNow);
            }

            return;
        }

        if (result.State == CheckoutFormPaymentState.Failed || result.FraudStatus == -1)
        {
            payment.MarkAsFailed(
                result.FailureReason ?? "Payment provider rejected the payment attempt.",
                result.ProviderPaymentId);
            return;
        }

        payment.RecordFraudStatus(result.FraudStatus ?? 0);
    }

    // Burada ödeme kaydını güvenli hosted form başlangıç DTO'suna dönüştürüyorum.
    private static CheckoutFormSessionDto ToSession(Payment payment)
    {
        return new CheckoutFormSessionDto(
            payment.Id,
            payment.OrderId,
            payment.Provider,
            payment.Status,
            payment.Amount,
            payment.PaymentPageUrl,
            payment.ProviderTokenExpiresAt);
    }

    // Burada callback sonucunu provider sırrı içermeyen küçük sonuç DTO'suna dönüştürüyorum.
    private static CheckoutFormCompletionDto ToCompletion(Payment payment)
    {
        return new CheckoutFormCompletionDto(payment.Id, payment.OrderId, payment.Status);
    }
}
