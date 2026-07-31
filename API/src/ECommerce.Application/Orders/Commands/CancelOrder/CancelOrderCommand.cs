using ECommerce.Application.Orders.Dtos;
using MediatR;

namespace ECommerce.Application.Orders.Commands.CancelOrder;

// Burada kullanıcının henüz ödenmemiş siparişini iptal etme isteğini taşıyorum.
public sealed record CancelOrderCommand(Guid OrderId) : IRequest<OrderDto>;
