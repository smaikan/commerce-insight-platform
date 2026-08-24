using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Payments;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Services;
using ECommerce.Application.Payments;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Orders.Commands.ExpireStockReservations;

public sealed class ExpireStockReservationsCommandHandler
    : IRequestHandler<ExpireStockReservationsCommand, StockReservationExpirationResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly OrderInventoryService _inventoryService;
    private readonly OrderCouponService _couponService;
    private readonly IReadOnlyCollection<IPaymentGatewayReconciler> _paymentReconcilers;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderNotificationService? _notificationService;
    private readonly DefinitivePaymentFailureService _definitivePaymentFailure;

    // Burada stok rezervasyonu sonlandırma akışının sipariş, stok, kupon, sağlayıcı, saat ve transaction bağımlılıklarını hazırlıyorum.
    public ExpireStockReservationsCommandHandler(
        IOrderRepository orderRepository,
        OrderInventoryService inventoryService,
        OrderCouponService couponService,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork,
        DefinitivePaymentFailureService definitivePaymentFailure,
        IEnumerable<IPaymentGatewayReconciler>? paymentReconcilers = null,
        IOrderNotificationService? notificationService = null)
    {
        _orderRepository = orderRepository;
        _inventoryService = inventoryService;
        _couponService = couponService;
        _paymentReconcilers = paymentReconcilers?.ToList() ?? [];
        _clock = clock;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _definitivePaymentFailure = definitivePaymentFailure;
    }

    // Burada süre dolan rezervasyonları önce sağlayıcı dışında çözüp ardından kısa serializable transactionlarla güvenle sonlandırıyorum.
    public async Task<StockReservationExpirationResult> Handle(
        ExpireStockReservationsCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = _clock.UtcNow;
        var orders = await _orderRepository.GetExpiredStockReservationsAsync(
            utcNow,
            request.BatchSize,
            cancellationToken);
        var cancelledOrderCount = 0;
        var skippedPendingPaymentCount = 0;
        var reconciledPaidOrderCount = 0;

        foreach (var order in orders)
        {
            var pendingPayments = order.Payments
                .Where(payment => payment.Status == PaymentStatus.Pending)
                .ToList();
            if (pendingPayments.Count == 0)
            {
                var outcome = await _unitOfWork.ExecuteInSerializableTransactionAsync(
                    transactionCancellationToken => ExpireWithoutPendingPaymentAsync(
                        order.Id,
                        utcNow,
                        transactionCancellationToken),
                    cancellationToken);
                cancelledOrderCount += outcome == ReservationOutcome.Cancelled ? 1 : 0;
                continue;
            }

            if (pendingPayments.Count != 1)
            {
                skippedPendingPaymentCount++;
                continue;
            }

            var pendingPayment = pendingPayments[0];
            var reconciliationResult = await ReconcilePendingPaymentOutsideTransactionAsync(
                order,
                pendingPayment,
                cancellationToken);
            if (reconciliationResult is null || reconciliationResult.Status == PaymentReconciliationStatus.Unknown)
            {
                skippedPendingPaymentCount++;
                continue;
            }

            var reconciledOutcome = await _unitOfWork.ExecuteInSerializableTransactionAsync(
                transactionCancellationToken => ApplyPaymentReconciliationAsync(
                    order.Id,
                    pendingPayment.Id,
                    reconciliationResult,
                    utcNow,
                    transactionCancellationToken),
                cancellationToken);
            cancelledOrderCount += reconciledOutcome == ReservationOutcome.Cancelled ? 1 : 0;
            reconciledPaidOrderCount += reconciledOutcome == ReservationOutcome.Paid ? 1 : 0;
        }

        return new StockReservationExpirationResult(
            cancelledOrderCount,
            skippedPendingPaymentCount,
            reconciledPaidOrderCount);
    }

    // Burada sağlanan idempotency anahtarıyla sağlayıcının bekleyen denemeyi kesin biçimde çözmesini transaction dışında istiyorum.
    private async Task<PaymentReconciliationResult?> ReconcilePendingPaymentOutsideTransactionAsync(
        Order order,
        Payment payment,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payment.IdempotencyKey))
        {
            return null;
        }

        var reconciler = _paymentReconcilers.FirstOrDefault(candidate => candidate.Provider == payment.Provider);
        if (reconciler is null)
        {
            return null;
        }

        try
        {
            return await reconciler.ReconcilePendingPaymentAsync(
                new PaymentReconciliationRequest(
                    order.Id,
                    payment.Id,
                    order.SubTotal,
                    payment.Amount,
                    payment.IdempotencyKey,
                    payment.ProviderToken),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new PaymentReconciliationResult(PaymentReconciliationStatus.Unknown);
        }
    }

    // Burada bekleyen ödeme bulunmayan süresi geçmiş siparişin stok ve kuponunu yalnız bir kez geri bırakıyorum.
    private async Task<ReservationOutcome> ExpireWithoutPendingPaymentAsync(
        Guid orderId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdForUpdateAsync(orderId, cancellationToken);
        if (order is null || !order.CanExpireStockReservation(utcNow))
        {
            return ReservationOutcome.None;
        }

        await _inventoryService.RestoreCancelledOrderStockAsync(order, cancellationToken);
        await _couponService.ReleaseForCancellationAsync(order, cancellationToken);
        if (!order.ExpireStockReservation(utcNow))
        {
            return ReservationOutcome.None;
        }

        if (_notificationService is not null)
        {
            await _notificationService.QueueOrderStatusChangedAsync(order, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ReservationOutcome.Cancelled;
    }

    // Burada sağlayıcının kesin sonucunu takipli aggregate'a uygulayıp ya ödemeyi kapatıyor ya da rezervasyonu serbest bırakıyorum.
    private async Task<ReservationOutcome> ApplyPaymentReconciliationAsync(
        Guid orderId,
        Guid paymentId,
        PaymentReconciliationResult reconciliationResult,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdForUpdateAsync(orderId, cancellationToken);
        if (order is null || !HasExpiredOpenReservation(order, utcNow))
        {
            return ReservationOutcome.None;
        }

        var payment = order.Payments.SingleOrDefault(candidate => candidate.Id == paymentId);
        if (payment is null || payment.Status != PaymentStatus.Pending)
        {
            return ReservationOutcome.None;
        }

        if (order.Payments.Any(candidate =>
                candidate.Id != payment.Id &&
                candidate.Status is PaymentStatus.Paid or PaymentStatus.Pending))
        {
            return ReservationOutcome.None;
        }

        if (reconciliationResult.Status == PaymentReconciliationStatus.Paid)
        {
            if (string.IsNullOrWhiteSpace(reconciliationResult.TransactionId))
            {
                return ReservationOutcome.None;
            }

            payment.MarkAsPaid(
                reconciliationResult.TransactionId,
                reconciliationResult.FraudStatus,
                reconciliationResult.ProviderPaidAmount,
                reconciliationResult.InstallmentCount);
            if (order.Status == OrderStatus.Pending)
            {
                order.ChangeStatus(OrderStatus.Confirmed, utcNow);
            }

            order.ChangeStatus(OrderStatus.Paid, utcNow);
            if (_notificationService is not null)
            {
                await _notificationService.QueuePaymentResultAsync(order, payment, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ReservationOutcome.Paid;
        }

        if (reconciliationResult.Status != PaymentReconciliationStatus.Cancelled)
        {
            return ReservationOutcome.None;
        }

        var applied = await _definitivePaymentFailure.ApplyAsync(
            order,
            payment,
            "Payment attempt was rejected during provider reconciliation.",
            reconciliationResult.TransactionId,
            cancellationToken);
        if (!applied)
        {
            return ReservationOutcome.None;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ReservationOutcome.Cancelled;
    }

    // Burada provider mutabakatı uygulanmadan önce siparişin hâlâ süresi geçmiş açık rezervasyon olduğunu doğruluyorum.
    private static bool HasExpiredOpenReservation(Order order, DateTime utcNow)
    {
        return order.ReservationExpiresAt.HasValue &&
               order.ReservationExpiresAt.Value <= utcNow &&
               order.Status is OrderStatus.Pending or OrderStatus.Confirmed;
    }

    private enum ReservationOutcome
    {
        None = 0,
        Cancelled = 1,
        Paid = 2
    }
}
