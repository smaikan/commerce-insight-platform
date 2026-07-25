using ECommerce.Application.Orders.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Orders.Commands.ChangeOrderStatus;

// Burada yöneticinin sipariş yaşam döngüsünde hedef duruma geçiş isteğini taşıyorum.
public sealed record ChangeOrderStatusCommand(Guid OrderId, OrderStatus Status) : IRequest<OrderDto>;
