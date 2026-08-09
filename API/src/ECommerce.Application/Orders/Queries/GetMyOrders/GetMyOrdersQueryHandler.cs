using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders.Dtos;
using MediatR;

namespace ECommerce.Application.Orders.Queries.GetMyOrders;

public sealed class GetMyOrdersQueryHandler : IRequestHandler<GetMyOrdersQuery, PagedResult<OrderSummaryDto>>
{
    private readonly IOrderListReader _orderListReader;
    private readonly ICurrentUserService _currentUser;

    // Burada kullanıcının yalnız kendi siparişlerini çözümlemek için repository ve kimlik servisini hazırlıyorum.
    public GetMyOrdersQueryHandler(IOrderListReader orderListReader, ICurrentUserService currentUser)
    {
        _orderListReader = orderListReader;
        _currentUser = currentUser;
    }

    // Burada owner filtresini repository katmanına zorunlu olarak aktararak sipariş özetlerini sayfalıyorum.
    public async Task<PagedResult<OrderSummaryDto>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        return await _orderListReader.GetListAsync(
            new OrderListFilter(
                request.PageNumber,
                request.PageSize,
                UserId: userId,
                Search: null,
                Status: request.Status,
                CreatedFromUtc: request.CreatedFromUtc,
                CreatedToUtc: request.CreatedToUtc),
            cancellationToken);
    }
}
