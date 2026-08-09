using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders.Dtos;
using MediatR;

namespace ECommerce.Application.Orders.Queries.GetOrders;

public sealed class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, PagedResult<OrderSummaryDto>>
{
    private readonly IOrderListReader _orderListReader;

    // Burada yönetim sipariş listesini almak için repository bağımlılığını hazırlıyorum.
    public GetOrdersQueryHandler(IOrderListReader orderListReader)
    {
        _orderListReader = orderListReader;
    }

    // Burada yönetim filtreleriyle sipariş özetlerini sayfalı olarak getiriyorum.
    public async Task<PagedResult<OrderSummaryDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        return await _orderListReader.GetListAsync(
            new OrderListFilter(
                request.PageNumber,
                request.PageSize,
                Search: request.Search,
                Status: request.Status,
                CreatedFromUtc: request.CreatedFromUtc,
                CreatedToUtc: request.CreatedToUtc),
            cancellationToken);
    }
}
