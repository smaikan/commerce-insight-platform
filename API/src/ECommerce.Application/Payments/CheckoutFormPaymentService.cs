using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Common.Payments;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Payments;

public sealed class CheckoutFormPaymentService : IPendingPaymentCancellationReconciler
{
    private readonly IOrderRepository _orders;
    private readonly IGuestOrderRepository _guestOrders;
    private readonly GuestOrders.GuestOrderAccessService _guestAccess;
    private readonly ICheckoutFormGateway _gateway;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderNotificationService _notifications;
    private readonly ICartRepository _carts;
    private readonly DefinitivePaymentFailureService _definitiveFailure;

    // Burada üye ve guest hosted ödeme akışının ortak bağımlılıklarını hazırlıyorum.
    public CheckoutFormPaymentService(
        IOrderRepository orders,
        IGuestOrderRepository guestOrders,
        GuestOrders.GuestOrderAccessService guestAccess,
        ICheckoutFormGateway gateway,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork,
        IOrderNotificationService notifications,
        ICartRepository carts,
        DefinitivePaymentFailureService definitiveFailure)
    {
        _orders = orders;
        _guestOrders = guestOrders;
        _guestAccess = guestAccess;
        _gateway = gateway;
        _currentUser = currentUser;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _notifications = notifications;
        _carts = carts;
        _definitiveFailure = definitiveFailure;
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
        return await CompleteByTokenAsync(token, null, cancellationToken);
    }

    // Burada sahiplik kapsamından gelen siparişin tek bekleyen iyzico denemesini provider sonucuyla uzlaştırıp iptal kararına güvenli durum döndürüyorum.
    public async Task<PaymentStatus?> ReconcileForCancellationAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        var pendingPayments = order.Payments
            .Where(payment => payment.Status == PaymentStatus.Pending)
            .ToList();
        if (pendingPayments.Count == 0)
        {
            return null;
        }

        if (pendingPayments.Count != 1)
        {
            throw new ConflictException("Multiple payment attempts require reconciliation before cancellation.");
        }

        var payment = pendingPayments[0];
        if (payment.Provider != _gateway.Provider ||
            string.IsNullOrWhiteSpace(payment.ProviderToken) ||
            string.IsNullOrWhiteSpace(payment.ProviderConversationId))
        {
            throw new ConflictException("The pending payment requires reconciliation before cancellation.");
        }

        try
        {
            var completion = await CompleteByTokenAsync(payment.ProviderToken, cancellationToken);
            return completion.Status;
        }
        catch (CheckoutFormProviderUnavailableException exception) when (
            string.Equals(exception.ErrorCode, "5122", StringComparison.Ordinal))
        {
            return PaymentStatus.Pending;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ConflictException(
                "The payment result is unknown and requires reconciliation before cancellation.",
                exception);
        }
    }

    // Burada callback veya webhook tokenını yerel conversation beklentisiyle birlikte güvenli biçimde sonuçlandırıyorum.
    private async Task<CheckoutFormCompletionDto> CompleteByTokenAsync(
        string token,
        string? webhookConversationId,
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
        if (webhookConversationId is not null &&
            !string.Equals(
                webhookConversationId,
                snapshotPayment.ProviderConversationId,
                StringComparison.Ordinal))
        {
            throw new ConflictException("Payment webhook conversation does not match the payment attempt.");
        }

        if (snapshotPayment.Status == PaymentStatus.Cancelled && snapshotPayment.CustomerAbandonedAt.HasValue)
        {
            await ReconcileAbandonedCheckoutFormAsync(normalizedToken, cancellationToken);
            return ToCompletion(snapshotPayment);
        }

        if (snapshotPayment.Status != PaymentStatus.Pending)
        {
            return ToCompletion(snapshotPayment);
        }

        var expectedConversationId = snapshotPayment.ProviderConversationId
            ?? throw new ConflictException("Payment provider conversation was not initialized.");
        var result = await _gateway.RetrieveAsync(
            normalizedToken,
            expectedConversationId,
            cancellationToken);
        EnsureRetrieveIdentityMatches(snapshotPayment, result);
        if (result.State == CheckoutFormPaymentState.Paid)
        {
            EnsureSuccessfulRetrieveMatches(snapshot, snapshotPayment, result);
        }

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

                await ApplyProviderResultAsync(
                    order,
                    payment,
                    result,
                    transactionCancellationToken);
                if (payment.Status == PaymentStatus.Paid && payment.ItemTransactions.Count == 0)
                {
                    var providerItems = MapAndValidateProviderItems(order, result);
                    var createdItems = payment.RecordProviderItemTransactions(providerItems, _clock.UtcNow);
                    await _orders.AddPaymentItemTransactionsAsync(createdItems, transactionCancellationToken);
                }

                if (payment.Status == PaymentStatus.Paid)
                {
                    await _notifications.QueuePaymentResultAsync(order, payment, transactionCancellationToken);
                }

                // Burada ödeme başarıyla onaylandığında müşterinin sepetini atomik olarak temizliyorum.
                if (payment.Status == PaymentStatus.Paid)
                {
                    await ClearCartForOrderAsync(order, transactionCancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
                return ToCompletion(payment);
            },
            cancellationToken);
    }

    // Burada müşteri tarafından terk edilmiş CheckoutForm tokenını izleyip geç tahsilatı siparişi diriltmeden iyzico'da geri çeviriyorum.
    public async Task<bool> ReconcileAbandonedCheckoutFormAsync(
        string providerToken,
        CancellationToken cancellationToken = default)
    {
        var utcNow = _clock.UtcNow;
        AbandonedCheckoutClaim? claim = null;
        var claimed = await _unitOfWork.ExecuteInSerializableTransactionAsync(
            async token =>
            {
                var order = await _orders.GetByPaymentProviderTokenAsync(providerToken, true, token);
                var payment = order?.Payments.SingleOrDefault(candidate => candidate.ProviderToken == providerToken);
                if (order is null || payment is null ||
                    !payment.ClaimAbandonmentReconciliation(utcNow, TimeSpan.FromMinutes(2)))
                {
                    return false;
                }

                claim = new AbandonedCheckoutClaim(order, payment);
                await _unitOfWork.SaveChangesAsync(token);
                return true;
            },
            cancellationToken);
        if (!claimed || claim is null)
        {
            return false;
        }

        CheckoutFormRetrieveResult result;
        try
        {
            result = await _gateway.RetrieveAsync(
                providerToken,
                claim.Payment.ProviderConversationId!,
                cancellationToken);
            EnsureRetrieveIdentityMatches(claim.Payment, result);
            if (result.State == CheckoutFormPaymentState.Paid)
            {
                EnsureSuccessfulRetrieveMatches(claim.Order, claim.Payment, result);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            await RescheduleAbandonedCheckoutFormAsync(providerToken, TimeSpan.FromMinutes(1), cancellationToken);
            return false;
        }

        if (result.State == CheckoutFormPaymentState.Paid &&
            result.FraudStatus == 1 &&
            !string.IsNullOrWhiteSpace(result.ProviderPaymentId) &&
            result.InstallmentCount.HasValue)
        {
            return await ReverseAbandonedLateChargeAsync(
                claim,
                result,
                cancellationToken);
        }

        if (result.State == CheckoutFormPaymentState.Failed || result.FraudStatus == -1 ||
            (claim.Payment.ProviderTokenExpiresAt.HasValue &&
             claim.Payment.ProviderTokenExpiresAt.Value.AddMinutes(5) <= utcNow))
        {
            await CompleteAbandonedCheckoutFormMonitoringAsync(providerToken, cancellationToken);
            return true;
        }

        await RescheduleAbandonedCheckoutFormAsync(providerToken, TimeSpan.FromMinutes(1), cancellationToken);
        return false;
    }

    // Burada geç tahsilatı provider dışında iptal edip başarılı sonucu kısa bir transaction ile ödeme denetimine kaydediyorum.
    private async Task<bool> ReverseAbandonedLateChargeAsync(
        AbandonedCheckoutClaim claim,
        CheckoutFormRetrieveResult result,
        CancellationToken cancellationToken)
    {
        LatePaymentReversalResult reversal;
        try
        {
            reversal = await _gateway.ReverseLatePaymentAsync(
                result.ProviderPaymentId!,
                $"abandon-{claim.Payment.Id:N}",
                claim.Payment.Amount,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            await RescheduleAbandonedCheckoutFormAsync(
                claim.Payment.ProviderToken!,
                TimeSpan.FromMinutes(1),
                cancellationToken);
            return false;
        }

        if (!reversal.Succeeded)
        {
            var retryDelay = reversal.Retryable ? TimeSpan.FromMinutes(1) : TimeSpan.FromMinutes(15);
            await RescheduleAbandonedCheckoutFormAsync(
                claim.Payment.ProviderToken!,
                retryDelay,
                cancellationToken);
            return false;
        }

        await _unitOfWork.ExecuteInSerializableTransactionAsync(
            async token =>
            {
                var order = await _orders.GetByPaymentProviderTokenAsync(
                    claim.Payment.ProviderToken!,
                    true,
                    token) ?? throw new ConflictException("Abandoned payment was not found.");
                var payment = order.Payments.Single(candidate =>
                    candidate.ProviderToken == claim.Payment.ProviderToken);
                if (!payment.AbandonmentReconciledAt.HasValue)
                {
                    payment.RecordReversedLateCharge(
                        result.ProviderPaymentId!,
                        result.FraudStatus,
                        result.PaidPrice,
                        result.InstallmentCount!.Value,
                        _clock.UtcNow);
                    await _unitOfWork.SaveChangesAsync(token);
                }

                return true;
            },
            cancellationToken);
        return true;
    }

    // Burada açık kalan terk edilmiş tokenı sonraki bounded worker turuna güvenli biçimde planlıyorum.
    private async Task RescheduleAbandonedCheckoutFormAsync(
        string providerToken,
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.ExecuteInSerializableTransactionAsync(
            async token =>
            {
                var order = await _orders.GetByPaymentProviderTokenAsync(providerToken, true, token);
                var payment = order?.Payments.SingleOrDefault(candidate => candidate.ProviderToken == providerToken);
                if (payment is null || payment.AbandonmentReconciledAt.HasValue)
                {
                    return false;
                }

                payment.ScheduleAbandonmentReconciliation(_clock.UtcNow.Add(delay));
                await _unitOfWork.SaveChangesAsync(token);
                return true;
            },
            cancellationToken);
    }

    // Burada tahsilat oluşmadan başarısız veya süresi dolmuş terk edilmiş tokenın izlenmesini terminal kapatıyorum.
    private async Task CompleteAbandonedCheckoutFormMonitoringAsync(
        string providerToken,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.ExecuteInSerializableTransactionAsync(
            async token =>
            {
                var order = await _orders.GetByPaymentProviderTokenAsync(providerToken, true, token);
                var payment = order?.Payments.SingleOrDefault(candidate => candidate.ProviderToken == providerToken);
                if (payment is null || payment.AbandonmentReconciledAt.HasValue)
                {
                    return false;
                }

                payment.CompleteAbandonmentReconciliation(_clock.UtcNow);
                await _unitOfWork.SaveChangesAsync(token);
                return true;
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

        return CompleteByTokenAsync(
            notification.Token,
            notification.PaymentConversationId,
            cancellationToken);
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
        catch (Exception exception)
        {
            throw new ConflictException(
                "Payment provider result is unknown and requires reconciliation.",
                exception);
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

                var expectedConversationId = payment.Id.ToString("N");
                if (result.Succeeded && result.Token is not null && result.PaymentPageUrl is not null &&
                    result.ExpiresAt.HasValue &&
                    string.Equals(result.ConversationId, expectedConversationId, StringComparison.Ordinal))
                {
                    payment.InitializeCheckoutForm(
                        result.Token,
                        payment.Id.ToString("N"),
                        result.PaymentPageUrl,
                        result.ExpiresAt.Value);
                }
                else if (result.IsDefinitiveFailure &&
                    !string.IsNullOrWhiteSpace(result.Token) &&
                    string.Equals(result.ConversationId, expectedConversationId, StringComparison.Ordinal))
                {
                    payment.RecordCheckoutFormIdentity(result.Token, expectedConversationId);
                    await _definitiveFailure.ApplyAsync(
                        order,
                        payment,
                        result.FailureReason ?? "Payment form could not be initialized.",
                        providerTransactionId: null,
                        token);
                }
                else
                {
                    throw new ConflictException(
                        "Payment provider result is unknown and requires reconciliation.");
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

    // Burada başarılı veya başarısız bütün retrieve sonuçlarının yerel token ve conversation kimliğine bağlanmasını zorluyorum.
    private static void EnsureRetrieveIdentityMatches(
        Payment payment,
        CheckoutFormRetrieveResult result)
    {
        if (!string.Equals(result.Token, payment.ProviderToken, StringComparison.Ordinal) ||
            !string.Equals(result.ConversationId, payment.ProviderConversationId, StringComparison.Ordinal))
        {
            throw new ConflictException("Payment provider result does not match the payment attempt.");
        }
    }

    // Burada imzalı başarılı retrieve sonucunun yerel sipariş ve tahsilat değişmezleriyle eşleşmesini zorunlu tutuyorum.
    private static void EnsureSuccessfulRetrieveMatches(
        Order order,
        Payment payment,
        CheckoutFormRetrieveResult result)
    {
        if (!string.Equals(result.BasketId, order.Id.ToString("N"), StringComparison.Ordinal) ||
            !string.Equals(result.Currency, "TRY", StringComparison.Ordinal) ||
            NormalizeCurrencyAmount(result.Price) != order.SubTotal ||
            NormalizeCurrencyAmount(result.PaidPrice) < payment.Amount ||
            !result.InstallmentCount.HasValue ||
            result.InstallmentCount is < 1 or > 12)
        {
            throw new ConflictException("Payment provider result does not match the order.");
        }

        MapAndValidateProviderItems(order, result);
    }

    // Burada CF-Retrieve kalemlerini sipariş item GUID'leri ve kuruşa dengelenmiş provider paidPrice dağılımıyla doğruluyorum.
    private static IReadOnlyList<ProviderPaymentItemSnapshot> MapAndValidateProviderItems(
        Order order,
        CheckoutFormRetrieveResult result)
    {
        var providerItems = result.ItemTransactions ?? [];
        if (providerItems.Count != order.Items.Count ||
            providerItems.Any(item =>
                string.IsNullOrWhiteSpace(item.ProviderPaymentTransactionId) ||
                !Guid.TryParseExact(item.ItemId, "N", out _) ||
                item.TransactionStatus is not 1 and not 2 ||
                item.Price <= 0m || item.PaidPrice <= 0m) ||
            providerItems.Select(item => item.ProviderPaymentTransactionId).Distinct(StringComparer.Ordinal).Count() != providerItems.Count ||
            providerItems.Select(item => item.ItemId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != providerItems.Count ||
            NormalizeCurrencyAmount(providerItems.Sum(item => item.Price)) != NormalizeCurrencyAmount(result.Price) ||
            NormalizeCurrencyAmount(providerItems.Sum(item => item.PaidPrice)) != NormalizeCurrencyAmount(result.PaidPrice))
        {
            throw new ConflictException("Payment provider item transactions do not match the order.");
        }

        var orderItems = order.Items.ToDictionary(item => item.Id);
        var normalizedPaidPrices = AllocateProviderPaidPrices(providerItems, result.PaidPrice);
        var snapshots = new List<ProviderPaymentItemSnapshot>(providerItems.Count);
        foreach (var providerItem in providerItems)
        {
            var orderItemId = Guid.ParseExact(providerItem.ItemId, "N");
            if (!orderItems.TryGetValue(orderItemId, out var orderItem) ||
                NormalizeCurrencyAmount(providerItem.Price) != orderItem.TotalPrice)
            {
                throw new ConflictException("Payment provider item transactions do not match the order items.");
            }

            snapshots.Add(new ProviderPaymentItemSnapshot(
                orderItemId,
                providerItem.ProviderPaymentTransactionId,
                NormalizeCurrencyAmount(providerItem.Price),
                normalizedPaidPrices[providerItem.ItemId]));
        }

        return snapshots;
    }

    // Burada iyzico'nun sekiz basamaklı kalem dağılımını toplamı kaybetmeden iki basamaklı TRY tutarlarına paylaştırıyorum.
    private static IReadOnlyDictionary<string, decimal> AllocateProviderPaidPrices(
        IReadOnlyList<CheckoutFormItemTransaction> providerItems,
        decimal providerPaidPrice)
    {
        var allocations = providerItems.ToDictionary(
            item => item.ItemId,
            item => decimal.Floor(item.PaidPrice * 100m) / 100m,
            StringComparer.OrdinalIgnoreCase);
        var target = NormalizeCurrencyAmount(providerPaidPrice);
        var remainingCents = decimal.ToInt32((target - allocations.Values.Sum()) * 100m);
        if (remainingCents < 0 || remainingCents > providerItems.Count)
        {
            throw new ConflictException("Payment provider item amounts cannot be normalized safely.");
        }

        foreach (var item in providerItems
                     .OrderByDescending(candidate => candidate.PaidPrice - allocations[candidate.ItemId])
                     .ThenBy(candidate => candidate.ItemId, StringComparer.OrdinalIgnoreCase)
                     .Take(remainingCents))
        {
            allocations[item.ItemId] += 0.01m;
        }

        if (allocations.Values.Any(amount => amount <= 0m) || allocations.Values.Sum() != target)
        {
            throw new ConflictException("Payment provider item amounts cannot be normalized safely.");
        }

        return allocations;
    }

    // Burada provider para değerlerini kalıcı modelin TRY kuruş hassasiyetine indiriyorum.
    private static decimal NormalizeCurrencyAmount(decimal amount)
    {
        return decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    // Burada doğrulanmış provider durumunu başarı, kesin başarısızlık veya belirsizlik semantiğiyle uygularım.
    private async Task ApplyProviderResultAsync(
        Order order,
        Payment payment,
        CheckoutFormRetrieveResult result,
        CancellationToken cancellationToken)
    {
        if (result.State == CheckoutFormPaymentState.Paid && result.FraudStatus == 1 &&
            !string.IsNullOrWhiteSpace(result.ProviderPaymentId) &&
            result.InstallmentCount.HasValue)
        {
            payment.MarkAsPaid(
                result.ProviderPaymentId,
                result.FraudStatus,
                NormalizeCurrencyAmount(result.PaidPrice),
                result.InstallmentCount.Value);
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
            await _definitiveFailure.ApplyAsync(
                order,
                payment,
                result.FailureReason ?? "Payment provider rejected the payment attempt.",
                result.ProviderPaymentId,
                cancellationToken);
            return;
        }

        if (result.FraudStatus.HasValue)
        {
            payment.RecordFraudStatus(result.FraudStatus.Value);
        }
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

    // Burada ödeme onaylandıktan sonra siparişi veren müşterinin sepetini atomik olarak temizliyorum.
    private async Task ClearCartForOrderAsync(Order order, CancellationToken cancellationToken)
    {
        CartOwner? owner = null;
        if (order.UserId.HasValue)
        {
            owner = CartOwner.ForUser(order.UserId.Value);
        }

        if (owner is null)
        {
            return;
        }

        var cart = await _carts.GetByOwnerForUpdateAsync(owner, cancellationToken);
        cart?.Clear();
    }

    private sealed record AbandonedCheckoutClaim(Order Order, Payment Payment);
}
