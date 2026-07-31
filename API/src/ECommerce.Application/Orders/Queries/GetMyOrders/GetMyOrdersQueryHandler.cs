using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders.Dtos;
using MediatR;

namespace ECommerce.Application.Orders.Queries.GetMyOrders;

public sealed class GetMyOrdersQueryHandler : IRequestHandler<GetMyOrdersQuery, PagedResult<OrderSummaryDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUserService _currentUser;

    // Burada kullanıcının yalnız kendi siparişlerini çözümlemek için repository ve kimlik servisini hazırlıyorum.
    public GetMyOrdersQueryHandler(IOrderRepository orderRepository, ICurrentUserService currentUser)
    {
        _orderRepository = orderRepository;
        _currentUser = currentUser;
    }

    // Burada owner filtresini repository katmanına zorunlu olarak aktararak sipariş özetlerini sayfalıyorum.
    public async Task<PagedResult<OrderSummaryDto>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var orders = await _orderRepository.GetListForUserAsync(
            new OrderListFilter(
                request.PageNumber,
                request.PageSize,
                Status: request.Status,
                CreatedFromUtc: request.CreatedFromUtc,
                CreatedToUtc: request.CreatedToUtc),
            userId,
            cancellationToken);
        return orders.Map(order => order.ToSummaryDto());
    }
}
