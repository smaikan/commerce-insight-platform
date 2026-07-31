using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders.Dtos;
using MediatR;

namespace ECommerce.Application.Orders.Queries.GetOrders;

public sealed class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, PagedResult<OrderSummaryDto>>
{
    private readonly IOrderRepository _orderRepository;

    // Burada yönetim sipariş listesini almak için repository bağımlılığını hazırlıyorum.
    public GetOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    // Burada yönetim filtreleriyle sipariş özetlerini sayfalı olarak getiriyorum.
    public async Task<PagedResult<OrderSummaryDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetListAsync(
            new OrderListFilter(
                request.PageNumber,
                request.PageSize,
                Status: request.Status,
                CreatedFromUtc: request.CreatedFromUtc,
                CreatedToUtc: request.CreatedToUtc),
            cancellationToken);
        return orders.Map(order => order.ToSummaryDto());
    }
}
