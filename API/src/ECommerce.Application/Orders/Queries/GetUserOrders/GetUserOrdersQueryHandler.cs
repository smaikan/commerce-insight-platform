using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders.Dtos;
using MediatR;

namespace ECommerce.Application.Orders.Queries.GetUserOrders;

// Burada kullanıcı kapsamlı sipariş özetini mevcut liste okuyucusundan güvenli owner filtresiyle getiriyorum.
public sealed class GetUserOrdersQueryHandler : IRequestHandler<GetUserOrdersQuery, PagedResult<OrderSummaryDto>>
{
    private readonly IOrderListReader _orderListReader;

    // Burada sipariş özetlerini aggregate grafiği yüklemeden okuyacak bağımlılığı hazırlıyorum.
    public GetUserOrdersQueryHandler(IOrderListReader orderListReader) => _orderListReader = orderListReader;

    // Burada seçili kullanıcı kimliğini liste filtresine zorunlu olarak aktarıyorum.
    public Task<PagedResult<OrderSummaryDto>> Handle(GetUserOrdersQuery request, CancellationToken cancellationToken) =>
        _orderListReader.GetListAsync(new OrderListFilter(request.PageNumber, request.PageSize, request.UserId, Status: request.Status, CreatedFromUtc: request.CreatedFromUtc, CreatedToUtc: request.CreatedToUtc), cancellationToken);
}
