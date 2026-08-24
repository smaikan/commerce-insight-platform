using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Payments;

public sealed class DefinitivePaymentFailureService
{
    private readonly OrderInventoryService _inventory;
    private readonly OrderCouponService _coupons;
    private readonly IOrderNotificationService _notifications;
    private readonly IDateTimeProvider _clock;

    // Burada kesin ödeme başarısızlığının stok, kupon, bildirim ve saat bağımlılıklarını hazırlıyorum.
    public DefinitivePaymentFailureService(
        OrderInventoryService inventory,
        OrderCouponService coupons,
        IOrderNotificationService notifications,
        IDateTimeProvider clock)
    {
        _inventory = inventory;
        _coupons = coupons;
        _notifications = notifications;
        _clock = clock;
    }

    // Burada doğrulanmış kesin ödeme başarısızlığını çağıranın transaction'ında bir kez sipariş iptaline dönüştürüyorum.
    public async Task<bool> ApplyAsync(
        Order order,
        Payment payment,
        string failureReason,
        string? providerTransactionId,
        CancellationToken cancellationToken)
    {
        if (payment.OrderId != order.Id || !order.Payments.Any(candidate => candidate.Id == payment.Id))
        {
            throw new ConflictException("Payment does not belong to the order.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return false;
        }

        if (payment.Status is PaymentStatus.Paid or PaymentStatus.Refunded ||
            order.Status is not OrderStatus.Pending and not OrderStatus.Confirmed)
        {
            return false;
        }

        if (order.Payments.Any(candidate =>
                candidate.Id != payment.Id &&
                candidate.Status is PaymentStatus.Paid or PaymentStatus.Pending))
        {
            return false;
        }

        var paymentJustFailed = payment.Status == PaymentStatus.Pending;
        if (paymentJustFailed)
        {
            payment.MarkAsFailed(failureReason, providerTransactionId);
        }
        else if (payment.Status != PaymentStatus.Failed)
        {
            return false;
        }

        await _inventory.RestoreCancelledOrderStockAsync(order, cancellationToken);
        await _coupons.ReleaseForCancellationAsync(order, cancellationToken);
        order.ChangeStatus(OrderStatus.Cancelled, _clock.UtcNow);
        if (paymentJustFailed)
        {
            await _notifications.QueuePaymentResultAsync(order, payment, cancellationToken);
        }

        await _notifications.QueueOrderStatusChangedAsync(order, cancellationToken);
        return true;
    }
}
