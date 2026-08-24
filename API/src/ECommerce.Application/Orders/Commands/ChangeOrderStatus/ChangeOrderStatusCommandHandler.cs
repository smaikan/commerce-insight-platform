using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Services;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Orders.Commands.ChangeOrderStatus;

public sealed class ChangeOrderStatusCommandHandler : IRequestHandler<ChangeOrderStatusCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly OrderInventoryService _inventoryService;
    private readonly OrderCouponService _couponService;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderNotificationService? _notificationService;
    private readonly IOrderCancellationOperationRepository _cancellationOperations;

    // Burada yönetim yaşam döngüsü değişikliği için sipariş, stok, saat ve transaction bağımlılıklarını hazırlıyorum.
    public ChangeOrderStatusCommandHandler(
        IOrderRepository orderRepository,
        OrderInventoryService inventoryService,
        OrderCouponService couponService,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork,
        IOrderCancellationOperationRepository cancellationOperations,
        IOrderNotificationService? notificationService = null)
    {
        _orderRepository = orderRepository;
        _inventoryService = inventoryService;
        _couponService = couponService;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _cancellationOperations = cancellationOperations;
        _notificationService = notificationService;
    }

    // Burada yetkili API sınırından gelen geçerli sipariş durum değişikliğini serializable transaction içinde uyguluyorum.
    public Task<OrderDto> Handle(ChangeOrderStatusCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCancellationToken => ChangeInTransactionAsync(request, transactionCancellationToken),
            cancellationToken);
    }

    // Burada iade akışını bypass eden durumları reddedip izinli yönetim geçişlerinde ödeme ve stok kurallarını koruyorum.
    private async Task<OrderDto> ChangeInTransactionAsync(
        ChangeOrderStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Status is OrderStatus.Refunded or
            OrderStatus.ReturnRequested or
            OrderStatus.ReturnApproved)
        {
            throw new ConflictException(
                "Refunded and return statuses cannot be set through the order status endpoint. " +
                "Use the dedicated return workflow or a provider-confirmed refund integration.");
        }

        var order = await _orderRepository.GetByIdForUpdateAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");
        if (request.Status == OrderStatus.Cancelled)
        {
            if (order.Status is not (OrderStatus.Pending or OrderStatus.Confirmed or OrderStatus.Paid or OrderStatus.Preparing))
            {
                throw new ConflictException("Only orders that have not been shipped can be cancelled.");
            }

            if (order.Payments.Any(payment => payment.Status == PaymentStatus.Pending))
            {
                throw new ConflictException("A payment attempt is still being processed and requires reconciliation before cancellation.");
            }

            if (order.Payments.Any(payment => payment.Status == PaymentStatus.Paid))
            {
                throw new ConflictException("Paid orders must be cancelled through a provider-confirmed payment reversal.");
            }

            await _inventoryService.RestoreCancelledOrderStockAsync(order, cancellationToken);
            await _couponService.ReleaseForCancellationAsync(order, cancellationToken);
        }

        if (request.Status == OrderStatus.Shipped)
        {
            var cancellationOperation = await _cancellationOperations.GetByOrderIdAsync(
                order.Id,
                true,
                cancellationToken);
            if (cancellationOperation?.Status is
                OrderCancellationOperationStatus.Requested or
                OrderCancellationOperationStatus.Processing or
                OrderCancellationOperationStatus.ReconciliationPending)
            {
                throw new ApiContractException(
                    409,
                    "order_cancellation_in_progress",
                    "Order cancellation is in progress",
                    "Ödeme geri alma işlemi devam eden sipariş kargoya verilemez.");
            }

            order.SetShipment(
                request.ShippingCarrier!,
                request.TrackingNumber!,
                request.TrackingUrl,
                _clock.UtcNow);
        }
        else
        {
            order.ChangeStatus(request.Status, _clock.UtcNow);
        }
        if (_notificationService is not null)
        {
            await _notificationService.QueueOrderStatusChangedAsync(order, cancellationToken);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return order.ToDto();
    }
}
