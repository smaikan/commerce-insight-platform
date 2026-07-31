using ECommerce.Application.Common.Models;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Orders.Queries.GetMyOrders;

// Burada oturum açmış kullanıcının kendi siparişlerini sayfalı getirme isteğini taşıyorum.
public sealed record GetMyOrdersQuery(
    int PageNumber = 1,
    int PageSize = 20,
    OrderStatus? Status = null,
    DateTime? CreatedFromUtc = null,
    DateTime? CreatedToUtc = null) : IRequest<PagedResult<OrderSummaryDto>>;
