using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders.Dtos;
using MediatR;

namespace ECommerce.Application.Orders.Queries.GetOrderByIdForAdmin;

public sealed class GetOrderByIdForAdminQueryHandler : IRequestHandler<GetOrderByIdForAdminQuery, OrderDto>
{
    private readonly IOrderRepository _orderRepository;

    // Burada yönetim sipariş detayı için repository bağımlılığını hazırlıyorum.
    public GetOrderByIdForAdminQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    // Burada yetkili API sınırı tarafından çağrılan sipariş detayını takip etmeden getiriyorum.
    public async Task<OrderDto> Handle(GetOrderByIdForAdminQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");
        return order.ToDto();
    }
}
