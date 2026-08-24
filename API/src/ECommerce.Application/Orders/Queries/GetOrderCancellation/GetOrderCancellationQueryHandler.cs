using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Services;
using MediatR;

namespace ECommerce.Application.Orders.Queries.GetOrderCancellation;

public sealed class GetOrderCancellationQueryHandler
    : IRequestHandler<GetOrderCancellationQuery, OrderCancellationOperationDto>
{
    private readonly IOrderRepository _orders;
    private readonly ICurrentUserService _currentUser;
    private readonly OrderCancellationService _cancellations;

    // Burada member owner-scoped cancellation polling sorgusunun bağımlılıklarını hazırlıyorum.
    public GetOrderCancellationQueryHandler(
        IOrderRepository orders,
        ICurrentUserService currentUser,
        OrderCancellationService cancellations)
    {
        _orders = orders;
        _currentUser = currentUser;
        _cancellations = cancellations;
    }

    // Burada siparişi yalnız JWT sahibi kapsamında doğrulayıp provider kimliği içermeyen operasyonu döndürüyorum.
    public async Task<OrderCancellationOperationDto> Handle(
        GetOrderCancellationQuery request,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdForUserAsync(
            request.OrderId,
            _currentUser.GetRequiredUserId(),
            cancellationToken) ?? throw new NotFoundException("Order was not found.");
        return await _cancellations.GetAsync(order.Id, "/api/orders", cancellationToken);
    }
}
