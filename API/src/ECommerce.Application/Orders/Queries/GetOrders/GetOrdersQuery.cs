using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Orders.Queries.GetOrders;

// Burada yönetim ekranı için tüm siparişleri sayfalı getirme isteğini taşıyorum.
public sealed record GetOrdersQuery(
    int PageNumber = 1,
    int PageSize = 20,
    OrderStatus? Status = null,
    DateTime? CreatedFromUtc = null,
    DateTime? CreatedToUtc = null) : IRequest<PagedResult<OrderSummaryDto>>;
