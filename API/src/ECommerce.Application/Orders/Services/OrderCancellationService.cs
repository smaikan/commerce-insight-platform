using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Payments;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Orders.Services;

public sealed class OrderCancellationService
{
    private static readonly TimeSpan OperationLease = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ReconciliationDelay = TimeSpan.FromMinutes(1);
    private readonly IOrderRepository _orders;
    private readonly IOrderCancellationOperationRepository _operations;
    private readonly IPendingPaymentCancellationReconciler _pendingPayments;
    private readonly ICheckoutFormGateway _gateway;
    private readonly OrderInventoryService _inventory;
    private readonly OrderCouponService _coupons;
    private readonly IOrderNotificationService _notifications;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthoritativeSalesMetricService _salesMetrics;

    // Burada müşteri iptal sagasının sahiplikten bağımsız finansal, stok, kupon ve outbox bağımlılıklarını hazırlıyorum.
    public OrderCancellationService(
        IOrderRepository orders,
        IOrderCancellationOperationRepository operations,
        IPendingPaymentCancellationReconciler pendingPayments,
        ICheckoutFormGateway gateway,
        OrderInventoryService inventory,
        OrderCouponService coupons,
        IOrderNotificationService notifications,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork,
        IAuthoritativeSalesMetricService salesMetrics)
    {
        _orders = orders;
        _operations = operations;
        _pendingPayments = pendingPayments;
        _gateway = gateway;
        _inventory = inventory;
        _coupons = coupons;
        _notifications = notifications;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _salesMetrics = salesMetrics;
    }

    // Burada sahibi önceden doğrulanmış siparişi ödeme durumuna göre doğrudan veya saga üzerinden iptal ediyorum.
    public async Task<OrderCancellationResult> RequestAsync(
        Order ownedSnapshot,
        OrderCancellationInitiatorType initiatorType,
        string pollingPathPrefix,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ownedSnapshot);
        EnsureCustomerCancellationStatus(ownedSnapshot.Status);
        if (ownedSnapshot.Status == OrderStatus.Cancelled)
        {
            return new OrderCancellationResult(ownedSnapshot.ToDto(), null);
        }

        if (ownedSnapshot.Status is OrderStatus.Pending or OrderStatus.Confirmed)
        {
            await _pendingPayments.ReconcileForCancellationAsync(ownedSnapshot, cancellationToken);
            var unpaidCancellation = await TryCancelWithoutCollectedPaymentAsync(
                ownedSnapshot.Id,
                cancellationToken);
            if (unpaidCancellation is not null)
            {
                return new OrderCancellationResult(unpaidCancellation, null);
            }
        }

        var operation = await CreateOrGetOperationAsync(
            ownedSnapshot.Id,
            initiatorType,
            cancellationToken);
        if (operation.Status == OrderCancellationOperationStatus.Failed ||
            operation.Status == OrderCancellationOperationStatus.ManualReview &&
            !CanRetryProviderVerification(operation))
        {
            ThrowTerminalOperation(operation);
        }

        await ProcessAsync(operation.Id, cancellationToken);
        var finalOrder = await _orders.GetByIdAsync(ownedSnapshot.Id, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");
        if (finalOrder.Status == OrderStatus.Cancelled)
        {
            return new OrderCancellationResult(finalOrder.ToDto(), null);
        }

        var current = await _operations.GetByIdAsync(operation.Id, false, cancellationToken)
            ?? throw new NotFoundException("Cancellation operation was not found.");
        if (current.Status is OrderCancellationOperationStatus.Failed or OrderCancellationOperationStatus.ManualReview)
        {
            ThrowTerminalOperation(current);
        }

        return new OrderCancellationResult(
            null,
            ToDto(current, pollingPathPrefix));
    }

    // Burada worker veya HTTP isteğinin tek operasyonu provider reporting ve ters işlem adımlarıyla ilerletmesini sağlıyorum.
    public async Task<bool> ProcessAsync(Guid operationId, CancellationToken cancellationToken = default)
    {
        var claimed = await _unitOfWork.ExecuteInSerializableTransactionAsync(
            async token =>
            {
                var operation = await _operations.GetByIdAsync(operationId, true, token);
                if (operation is null)
                {
                    return false;
                }

                if (CanRetryProviderVerification(operation))
                {
                    var order = await _orders.GetByIdForUpdateAsync(operation.OrderId, token)
                        ?? throw new NotFoundException("Order was not found.");
                    var payment = order.Payments.Single(candidate => candidate.Id == operation.PaymentId);
                    if (order.Status is not OrderStatus.Paid and not OrderStatus.Preparing ||
                        payment.Status != PaymentStatus.Paid)
                    {
                        return false;
                    }

                    operation.RequeueManualReview(_clock.UtcNow);
                }

                if (!operation.TryClaim(_clock.UtcNow, OperationLease))
                {
                    return false;
                }

                await _unitOfWork.SaveChangesAsync(token);
                return true;
            },
            cancellationToken);
        if (!claimed)
        {
            return false;
        }

        var operationSnapshot = await _operations.GetByIdAsync(operationId, false, cancellationToken)
            ?? throw new NotFoundException("Cancellation operation was not found.");
        var orderSnapshot = await _orders.GetByIdAsync(operationSnapshot.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");
        var payment = orderSnapshot.Payments.Single(candidate => candidate.Id == operationSnapshot.PaymentId);

        PaymentReversalReport report;
        try
        {
            report = await _gateway.RetrieveReversalReportAsync(
                operationSnapshot.ProviderPaymentId,
                cancellationToken);
            ValidateReport(operationSnapshot, orderSnapshot, payment, report);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            await MarkReconciliationPendingAsync(operationId, "provider_unavailable", cancellationToken);
            return false;
        }
        catch (Exception)
        {
            await MarkManualReviewAsync(
                operationId,
                OrderCancellationOperation.ProviderResponseMismatchErrorCode,
                "Provider reconciliation response could not be verified.",
                cancellationToken);
            return false;
        }

        if (await ApplyReportingEvidenceAsync(operationId, report, cancellationToken))
        {
            return await CompleteLocalEffectsAsync(operationId, cancellationToken);
        }

        operationSnapshot = await _operations.GetByIdAsync(operationId, false, cancellationToken)
            ?? throw new NotFoundException("Cancellation operation was not found.");
        return operationSnapshot.ReversalType == PaymentReversalType.Cancel
            ? await ProcessCancelAsync(operationSnapshot, cancellationToken)
            : await ProcessRefundAsync(operationSnapshot, cancellationToken);
    }

    // Burada sahibi doğrulanmış siparişin güncel cancellation operasyonunu polling cevabına dönüştürüyorum.
    public async Task<OrderCancellationOperationDto> GetAsync(
        Guid orderId,
        string pollingPathPrefix,
        CancellationToken cancellationToken)
    {
        var operation = await _operations.GetByOrderIdAsync(orderId, false, cancellationToken)
            ?? throw new NotFoundException("Cancellation operation was not found.");
        return ToDto(operation, pollingPathPrefix);
    }

    // Burada tahsilat oluşmamış siparişin mevcut stok, kupon ve abandoned token davranışını atomik koruyorum.
    private Task<OrderDto?> TryCancelWithoutCollectedPaymentAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInSerializableTransactionAsync<OrderDto?>(
            async token =>
            {
                var order = await _orders.GetByIdForUpdateAsync(orderId, token)
                    ?? throw new NotFoundException("Order was not found.");
                if (order.Status == OrderStatus.Cancelled)
                {
                    return order.ToDto();
                }

                if (order.Status is OrderStatus.Paid or OrderStatus.Preparing)
                {
                    return null;
                }

                EnsureCustomerCancellationStatus(order.Status);
                if (order.Payments.Any(payment => payment.Status == PaymentStatus.Paid))
                {
                    return null;
                }

                foreach (var pendingPayment in order.Payments.Where(payment => payment.Status == PaymentStatus.Pending))
                {
                    pendingPayment.AbandonCheckoutForm(_clock.UtcNow);
                }

                await _inventory.RestoreCancelledOrderStockAsync(order, token);
                await _coupons.ReleaseForCancellationAsync(order, token);
                order.ChangeStatus(OrderStatus.Cancelled, _clock.UtcNow);
                await _notifications.QueueOrderStatusChangedAsync(order, token);
                await _unitOfWork.SaveChangesAsync(token);
                return order.ToDto();
            },
            cancellationToken);
    }

    // Burada paid/preparing sipariş için tek aktif cancellation intent'ini serializable transaction içinde oluşturuyorum.
    private Task<OrderCancellationOperation> CreateOrGetOperationAsync(
        Guid orderId,
        OrderCancellationInitiatorType initiatorType,
        CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            async token =>
            {
                var order = await _orders.GetByIdForUpdateAsync(orderId, token)
                    ?? throw new NotFoundException("Order was not found.");
                if (order.Status == OrderStatus.Cancelled)
                {
                    var completed = await _operations.GetByOrderIdAsync(orderId, true, token);
                    return completed ?? throw new ConflictException("Cancelled order operation was not found.");
                }

                EnsureCustomerCancellationStatus(order.Status);
                if (order.Status is not OrderStatus.Paid and not OrderStatus.Preparing)
                {
                    throw new ConflictException("The payment state must be reconciled before cancellation.");
                }

                order.RegisterCancellationIntent();

                var existing = await _operations.GetByOrderIdAsync(orderId, true, token);
                if (existing is not null)
                {
                    return existing;
                }

                var payment = order.Payments.SingleOrDefault(candidate => candidate.Status == PaymentStatus.Paid)
                    ?? throw new ConflictException("A paid payment was not found for cancellation.");
                if (payment.Provider != PaymentProvider.Iyzico || string.IsNullOrWhiteSpace(payment.TransactionId) ||
                    !payment.ProviderPaidAmount.HasValue || string.IsNullOrWhiteSpace(payment.ProviderConversationId))
                {
                    throw new ApiContractException(
                        409,
                        "payment_reversal_data_missing",
                        "Payment reversal data missing",
                        "Ödeme güvenli biçimde geri alınabilmek için gerekli sağlayıcı bilgilerini içermiyor.");
                }

                var reversalType = IsSameIyzicoBusinessDate(payment.PaidAt, _clock.UtcNow)
                    ? PaymentReversalType.Cancel
                    : PaymentReversalType.Refund;
                if (reversalType == PaymentReversalType.Refund && payment.ItemTransactions.Count == 0)
                {
                    throw new ApiContractException(
                        409,
                        "payment_reversal_data_missing",
                        "Payment reversal data missing",
                        "Standart refund için item-level provider transaction bilgileri bulunamadı.");
                }

                OrderCancellationOperation operation;
                try
                {
                    operation = new OrderCancellationOperation(order, payment, initiatorType, reversalType, _clock.UtcNow);
                }
                catch (DomainException exception)
                {
                    throw new ConflictException("Payment cancellation operation could not be created.", exception);
                }

                await _operations.AddAsync(operation, token);
                await _unitOfWork.SaveChangesAsync(token);
                return operation;
            },
            cancellationToken);
    }

    // Burada reporting'de bu operasyona ait tam cancel veya item refund kanıtlarını yerel audit kayıtlarına işlerim.
    private async Task<bool> ApplyReportingEvidenceAsync(
        Guid operationId,
        PaymentReversalReport report,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(
            async token =>
            {
                var operation = await _operations.GetByIdAsync(operationId, true, token)
                    ?? throw new NotFoundException("Cancellation operation was not found.");
                if (operation.Status != OrderCancellationOperationStatus.Processing)
                {
                    return operation.Status == OrderCancellationOperationStatus.Completed;
                }

                if (operation.ReversalType == PaymentReversalType.Cancel)
                {
                    var cancelFound = report.Cancels.Any(cancel =>
                        cancel.ConversationId == operation.ProviderConversationId &&
                        cancel.Amount == operation.Payment.ProviderPaidAmount &&
                        cancel.Currency == "TRY" &&
                        cancel.Status == 1);
                    return cancelFound;
                }

                foreach (var operationItem in operation.Items.Where(item => item.Status != PaymentReversalItemStatus.Completed))
                {
                    var reportItem = report.Items.SingleOrDefault(item =>
                        item.ProviderPaymentTransactionId == operationItem.ProviderPaymentTransactionId);
                    var refundFound = reportItem?.Refunds.Any(refund =>
                        refund.ConversationId == operationItem.ProviderConversationId &&
                        refund.Amount == operationItem.Amount &&
                        refund.Currency == "TRY" &&
                        refund.Status == 1) == true;
                    if (refundFound)
                    {
                        operationItem.MarkCompleted(_clock.UtcNow);
                    }
                }

                await _unitOfWork.SaveChangesAsync(token);
                return operation.Items.All(item => item.Status == PaymentReversalItemStatus.Completed) &&
                    string.Equals(report.RefundStatus, "TOTALLY_REFUNDED", StringComparison.OrdinalIgnoreCase);
            },
            cancellationToken);
    }

    // Burada aynı gün cancel isteğini gönderip kesin ret halinde item-level refund'a güvenli geçiş yapıyorum.
    private async Task<bool> ProcessCancelAsync(
        OrderCancellationOperation operation,
        CancellationToken cancellationToken)
    {
        PaymentReversalGatewayResult result;
        try
        {
            result = await _gateway.CancelPaymentAsync(
                operation.ProviderPaymentId,
                operation.ProviderConversationId,
                operation.Payment.ProviderPaidAmount!.Value,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            await MarkReconciliationPendingAsync(operation.Id, "provider_timeout", cancellationToken);
            return false;
        }
        catch (Exception)
        {
            await MarkReconciliationPendingAsync(
                operation.Id,
                OrderCancellationOperation.ProviderResponseMismatchErrorCode,
                cancellationToken);
            return false;
        }

        if (result.Succeeded)
        {
            return await CompleteLocalEffectsAsync(operation.Id, cancellationToken);
        }

        if (result.Retryable)
        {
            await MarkReconciliationPendingAsync(operation.Id, result.ErrorCode, cancellationToken);
            return false;
        }

        var switched = await TrySwitchToRefundAsync(operation.Id, result.ErrorCode, cancellationToken);
        if (!switched)
        {
            return false;
        }

        var updated = await _operations.GetByIdAsync(operation.Id, false, cancellationToken)
            ?? throw new NotFoundException("Cancellation operation was not found.");
        return await ProcessRefundAsync(updated, cancellationToken);
    }

    // Burada her item refund'ını ayrı provider conversation ile gönderip başarıyı hemen kalıcı audit kaydına alıyorum.
    private async Task<bool> ProcessRefundAsync(
        OrderCancellationOperation operation,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var item = await ClaimNextRefundItemAsync(operation.Id, cancellationToken);
            if (item is null)
            {
                var current = await _operations.GetByIdAsync(operation.Id, false, cancellationToken)
                    ?? throw new NotFoundException("Cancellation operation was not found.");
                if (current.Items.All(candidate => candidate.Status == PaymentReversalItemStatus.Completed))
                {
                    return await CompleteLocalEffectsAsync(operation.Id, cancellationToken);
                }

                return false;
            }

            PaymentReversalGatewayResult result;
            try
            {
                result = await _gateway.RefundPaymentItemAsync(
                    operation.ProviderPaymentId,
                    item.ProviderPaymentTransactionId,
                    item.ProviderConversationId,
                    item.Amount,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException)
            {
                await MarkRefundItemPendingAsync(operation.Id, item.Id, "provider_timeout", cancellationToken);
                return false;
            }
            catch (Exception)
            {
                await MarkRefundItemPendingAsync(
                    operation.Id,
                    item.Id,
                    OrderCancellationOperation.ProviderResponseMismatchErrorCode,
                    cancellationToken);
                return false;
            }

            if (!result.Succeeded)
            {
                if (result.Retryable)
                {
                    await MarkRefundItemPendingAsync(operation.Id, item.Id, result.ErrorCode, cancellationToken);
                }
                else
                {
                    await MarkRefundItemFailedAsync(operation.Id, item.Id, result.ErrorCode, cancellationToken);
                }

                return false;
            }

            await MarkRefundItemCompletedAsync(operation.Id, item.Id, cancellationToken);
        }
    }

    // Burada provider başarısından sonra bütün yerel yan etkileri tek SaveChanges ile atomik tamamlıyorum.
    private Task<bool> CompleteLocalEffectsAsync(Guid operationId, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            async token =>
            {
                var operation = await _operations.GetByIdAsync(operationId, true, token)
                    ?? throw new NotFoundException("Cancellation operation was not found.");
                var order = await _orders.GetByIdForUpdateAsync(operation.OrderId, token)
                    ?? throw new NotFoundException("Order was not found.");
                if (operation.Status == OrderCancellationOperationStatus.Completed && order.Status == OrderStatus.Cancelled)
                {
                    return true;
                }

                if (order.Status is OrderStatus.Shipped or OrderStatus.Delivered)
                {
                    operation.MarkManualReview(
                        _clock.UtcNow,
                        "order_lifecycle_advanced",
                        "Order advanced to shipment after payment reversal intent.");
                    await _unitOfWork.SaveChangesAsync(token);
                    return false;
                }

                var payment = order.Payments.Single(candidate => candidate.Id == operation.PaymentId);
                if (payment.Status == PaymentStatus.Paid)
                {
                    if (operation.ReversalType == PaymentReversalType.Cancel)
                    {
                        payment.MarkAsCancelledAfterProviderReversal();
                    }
                    else
                    {
                        payment.MarkAsRefunded();
                    }
                }

                await _inventory.RestoreCancelledOrderStockAsync(order, token);
                await _coupons.ReleaseForCancellationAsync(order, token);
                order.ChangeStatus(OrderStatus.Cancelled, _clock.UtcNow);
                operation.MarkCompleted(operation.ReversalType, _clock.UtcNow);
                await _salesMetrics.ReverseCancelledOrderAsync(order, token);
                await _notifications.QueueOrderStatusChangedAsync(order, token);
                await _notifications.QueuePaymentReversalCompletedAsync(order, payment, operation, token);
                await _unitOfWork.SaveChangesAsync(token);
                return true;
            },
            cancellationToken);
    }

    // Burada kesin cancel reddinden sonra eksiksiz provider item snapshot'ı varsa refund operasyonuna geçiyorum.
    private Task<bool> TrySwitchToRefundAsync(
        Guid operationId,
        string? errorCode,
        CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            async token =>
            {
                var operation = await _operations.GetByIdAsync(operationId, true, token)
                    ?? throw new NotFoundException("Cancellation operation was not found.");
                if (operation.Payment.ItemTransactions.Count == 0)
                {
                    operation.MarkManualReview(
                        _clock.UtcNow,
                        "payment_reversal_data_missing",
                        "Cancel was rejected and item-level refund data is unavailable.");
                    await _unitOfWork.SaveChangesAsync(token);
                    return false;
                }

                operation.SwitchToRefund(operation.Payment.ItemTransactions, _clock.UtcNow);
                await _unitOfWork.SaveChangesAsync(token);
                return true;
            },
            cancellationToken);
    }

    // Burada sıradaki refund kalemini provider çağrısından önce veritabanında işleniyor olarak işaretliyorum.
    private Task<OrderCancellationOperationItem?> ClaimNextRefundItemAsync(
        Guid operationId,
        CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            async token =>
            {
                var operation = await _operations.GetByIdAsync(operationId, true, token)
                    ?? throw new NotFoundException("Cancellation operation was not found.");
                var item = operation.ClaimNextRefundItem(_clock.UtcNow);
                await _unitOfWork.SaveChangesAsync(token);
                return item;
            },
            cancellationToken);
    }

    // Burada başarılı item refund sonucunu tekrar gönderilmeyecek biçimde kalıcılaştırıyorum.
    private Task MarkRefundItemCompletedAsync(Guid operationId, Guid itemId, CancellationToken cancellationToken)
    {
        return UpdateRefundItemAsync(operationId, itemId, (item, now) => item.MarkCompleted(now), cancellationToken);
    }

    // Burada timeout alan refund kalemini reporting uzlaştırması bekleyen duruma alıyorum.
    private Task MarkRefundItemPendingAsync(Guid operationId, Guid itemId, string? code, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            async token =>
            {
                var operation = await _operations.GetByIdAsync(operationId, true, token)
                    ?? throw new NotFoundException("Cancellation operation was not found.");
                operation.Items.Single(item => item.Id == itemId).MarkReconciliationPending(_clock.UtcNow, code);
                operation.MarkReconciliationPending(
                    _clock.UtcNow,
                    _clock.UtcNow.Add(ReconciliationDelay),
                    code,
                    "Item refund result is awaiting provider reporting reconciliation.");
                await _unitOfWork.SaveChangesAsync(token);
                return true;
            },
            cancellationToken);
    }

    // Burada kesin reddedilen refund kalemini terminal işaretleyip operasyonu manual review'a alıyorum.
    private async Task MarkRefundItemFailedAsync(Guid operationId, Guid itemId, string? code, CancellationToken cancellationToken)
    {
        await _unitOfWork.ExecuteInSerializableTransactionAsync(
            async token =>
            {
                var operation = await _operations.GetByIdAsync(operationId, true, token)
                    ?? throw new NotFoundException("Cancellation operation was not found.");
                operation.Items.Single(item => item.Id == itemId).MarkFailed(_clock.UtcNow, code);
                operation.MarkManualReview(
                    _clock.UtcNow,
                    code ?? "provider_refund_rejected",
                    "Provider definitively rejected an item refund.");
                await _unitOfWork.SaveChangesAsync(token);
                return true;
            },
            cancellationToken);
    }

    // Burada item-level audit mutasyonlarını küçük serializable transaction içinde ortaklaştırıyorum.
    private async Task UpdateRefundItemAsync(
        Guid operationId,
        Guid itemId,
        Action<OrderCancellationOperationItem, DateTime> update,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.ExecuteInSerializableTransactionAsync(
            async token =>
            {
                var operation = await _operations.GetByIdAsync(operationId, true, token)
                    ?? throw new NotFoundException("Cancellation operation was not found.");
                update(operation.Items.Single(item => item.Id == itemId), _clock.UtcNow);
                await _unitOfWork.SaveChangesAsync(token);
                return true;
            },
            cancellationToken);
    }

    // Burada belirsiz operasyonu güvenli hata özeti ve bounded tekrar zamanı ile worker'a bırakıyorum.
    private async Task MarkReconciliationPendingAsync(Guid operationId, string? errorCode, CancellationToken cancellationToken)
    {
        await _unitOfWork.ExecuteInSerializableTransactionAsync(
            async token =>
            {
                var operation = await _operations.GetByIdAsync(operationId, true, token)
                    ?? throw new NotFoundException("Cancellation operation was not found.");
                operation.MarkReconciliationPending(
                    _clock.UtcNow,
                    _clock.UtcNow.Add(ReconciliationDelay),
                    errorCode,
                    "Provider reversal result is awaiting reconciliation.");
                await _unitOfWork.SaveChangesAsync(token);
                return true;
            },
            cancellationToken);
    }

    // Burada bütünlüğü doğrulanamayan finansal cevabı otomatik retry yerine operatör incelemesine alıyorum.
    private async Task MarkManualReviewAsync(
        Guid operationId,
        string errorCode,
        string summary,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.ExecuteInSerializableTransactionAsync(
            async token =>
            {
                var operation = await _operations.GetByIdAsync(operationId, true, token)
                    ?? throw new NotFoundException("Cancellation operation was not found.");
                operation.MarkManualReview(_clock.UtcNow, errorCode, summary);
                await _unitOfWork.SaveChangesAsync(token);
                return true;
            },
            cancellationToken);
    }

    // Burada yalnız ilk provider ön-kontrolünde teknik hata alan operasyonun tek güvenli retry hakkını belirliyorum.
    private static bool CanRetryProviderVerification(OrderCancellationOperation operation)
    {
        return operation.Status == OrderCancellationOperationStatus.ManualReview &&
            operation.AttemptCount < OrderCancellationOperation.MaximumProviderVerificationAttempts &&
            string.Equals(
                operation.ErrorCode,
                OrderCancellationOperation.ProviderResponseMismatchErrorCode,
                StringComparison.Ordinal);
    }

    // Burada reporting cevabının yerel sipariş, ödeme ve gerçek provider tutarlarıyla birebir eşleşmesini doğruluyorum.
    private static void ValidateReport(
        OrderCancellationOperation operation,
        Order order,
        Payment payment,
        PaymentReversalReport report)
    {
        if (report.ProviderPaymentId != operation.ProviderPaymentId ||
            report.PaymentConversationId != payment.ProviderConversationId ||
            report.Currency != "TRY" ||
            report.Price != order.SubTotal ||
            report.PaidPrice != payment.ProviderPaidAmount)
        {
            throw new InvalidOperationException("Provider reversal report does not match the local payment.");
        }
    }

    // Burada müşteri iptalini yalnız Shipped öncesi durumlarla sınırlandırıp kararlı typed hata üretiyorum.
    private static void EnsureCustomerCancellationStatus(OrderStatus status)
    {
        if (status is OrderStatus.Pending or OrderStatus.Confirmed or OrderStatus.Paid or OrderStatus.Preparing or OrderStatus.Cancelled)
        {
            return;
        }

        throw new ApiContractException(
            409,
            "order_cancellation_not_allowed",
            "Order cannot be cancelled",
            "Kargoya verilmiş veya tamamlanmış sipariş müşteri tarafından iptal edilemez.");
    }

    // Burada iyzico aynı gün cancel kararını Türkiye iş tarihine göre hesaplıyorum.
    private static bool IsSameIyzicoBusinessDate(DateTime? paidAtUtc, DateTime utcNow)
    {
        if (!paidAtUtc.HasValue)
        {
            return false;
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        return TimeZoneInfo.ConvertTimeFromUtc(paidAtUtc.Value, timeZone).Date ==
            TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone).Date;
    }

    // Burada terminal provider sonucunu güvenli ve kararlı 409 ProblemDetails sözleşmesine dönüştürüyorum.
    private static void ThrowTerminalOperation(OrderCancellationOperation operation)
    {
        throw new ApiContractException(
            409,
            operation.Status == OrderCancellationOperationStatus.ManualReview
                ? "payment_reversal_manual_review"
                : "payment_reversal_rejected",
            "Payment reversal could not be completed",
            operation.Status == OrderCancellationOperationStatus.ManualReview
                ? "Ödeme geri alma işlemi manuel inceleme gerektiriyor."
                : "Ödeme sağlayıcısı geri alma işlemini kesin olarak reddetti.");
    }

    // Burada operasyonu provider kimliklerini açmadan owner-scoped polling DTO'suna dönüştürüyorum.
    private static OrderCancellationOperationDto ToDto(
        OrderCancellationOperation operation,
        string pollingPathPrefix)
    {
        return new OrderCancellationOperationDto(
            operation.Id,
            operation.OrderId,
            operation.Status,
            operation.ReversalType,
            operation.CreatedAt,
            operation.UpdatedAt,
            operation.NextAttemptAt,
            $"{pollingPathPrefix.TrimEnd('/')}/{operation.OrderId}/cancellation");
    }
}
