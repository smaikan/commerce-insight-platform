using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Orders.Queries.GetUserOrders;

// Burada yöneticinin seçili kullanıcıya ait sipariş özetlerini sayfalı almasını tanımlıyorum.
public sealed record GetUserOrdersQuery(
    long UserId,
    int PageNumber = 1,
    int PageSize = 20,
    OrderStatus? Status = null,
    DateTime? CreatedFromUtc = null,
    DateTime? CreatedToUtc = null) : IRequest<PagedResult<OrderSummaryDto>>;
