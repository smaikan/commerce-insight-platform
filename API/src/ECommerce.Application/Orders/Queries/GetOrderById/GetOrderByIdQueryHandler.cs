using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders.Dtos;
using MediatR;

namespace ECommerce.Application.Orders.Queries.GetOrderById;

public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUser;

    // Burada sipariş detayını yalnız sahibine döndürmek için repository ve kimlik servisini hazırlıyorum.
    public GetOrderByIdQueryHandler(IOrderRepository orderRepository, ICurrentUserService currentUser)
    {
        _orderRepository = orderRepository;
        _currentUser = currentUser;
    }

    // Burada owner-scope sorgusuyla sipariş detayını getirip başka kullanıcının varlığını sızdırmıyorum.
    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdForUserAsync(
            request.OrderId,
            _currentUser.GetRequiredUserId(),
            cancellationToken)
            ?? throw new NotFoundException("Order was not found.");
        return order.ToDto();
    }
}
