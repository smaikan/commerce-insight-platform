using ECommerce.Application.Orders.Dtos;
using MediatR;

namespace ECommerce.Application.Orders.Queries.GetOrderById;

// Burada kullanıcının yalnız kendi sipariş detayını getirme isteğini taşıyorum.
public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto>;
