using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Services;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Orders.Commands.CancelOrder;

public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, OrderCancellationResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly OrderCancellationService _cancellations;

    // Burada member sahiplik kontrolü ile ortak cancellation sagasını hazırlıyorum.
    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        ICurrentUserService currentUser,
        OrderCancellationService cancellations)
    {
        _orderRepository = orderRepository;
        _currentUser = currentUser;
        _cancellations = cancellations;
    }

    // Burada üye sahipliğini doğrulayıp ortak cancellation sagasını member polling sözleşmesiyle çalıştırıyorum.
    public async Task<OrderCancellationResult> Handle(
        CancelOrderCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var snapshot = await _orderRepository.GetByIdForUserAsync(request.OrderId, userId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");
        return await _cancellations.RequestAsync(
            snapshot,
            OrderCancellationInitiatorType.Member,
            "/api/orders",
            cancellationToken);
    }
}
