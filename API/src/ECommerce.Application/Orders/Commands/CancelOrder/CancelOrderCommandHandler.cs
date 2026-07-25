using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Services;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Orders.Commands.CancelOrder;

public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly OrderInventoryService _inventoryService;
    private readonly OrderCouponService _couponService;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderNotificationService? _notificationService;

    // Burada kullanıcının sipariş iptali için gereken sipariş, stok, kimlik, saat ve transaction bağımlılıklarını hazırlıyorum.
    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        OrderInventoryService inventoryService,
        OrderCouponService couponService,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork,
        IOrderNotificationService? notificationService = null)
    {
        _orderRepository = orderRepository;
        _inventoryService = inventoryService;
        _couponService = couponService;
        _currentUser = currentUser;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    // Burada yalnız sahibinin ödeme öncesi siparişini stokları geri ekleyerek iptal ediyorum.
    public Task<OrderDto> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCancellationToken => CancelInTransactionAsync(request.OrderId, userId, transactionCancellationToken),
            cancellationToken);
    }

    // Burada iptalin geçerli durumda yapıldığını, stokların bir kez geri geldiğini ve ödemelerin iptal edildiğini doğruluyorum.
    private async Task<OrderDto> CancelInTransactionAsync(Guid orderId, long userId, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdForUserForUpdateAsync(orderId, userId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");
        if (order.Status is not OrderStatus.Pending and not OrderStatus.Confirmed)
        {
            throw new ConflictException("Only pending or confirmed orders can be cancelled by the customer.");
        }

        if (order.Payments.Any(payment => payment.Status == PaymentStatus.Pending))
        {
            throw new ConflictException("A payment attempt is still being processed and requires reconciliation before cancellation.");
        }

        await _inventoryService.RestoreCancelledOrderStockAsync(order, cancellationToken);
        await _couponService.ReleaseForCancellationAsync(order, cancellationToken);
        order.ChangeStatus(OrderStatus.Cancelled, _clock.UtcNow);
        if (_notificationService is not null)
        {
            await _notificationService.QueueOrderStatusChangedAsync(order, cancellationToken);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return order.ToDto();
    }
}
