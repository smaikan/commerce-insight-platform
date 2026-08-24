using ECommerce.Application.Orders.Dtos;
using MediatR;

namespace ECommerce.Application.Orders.Commands.CancelOrder;

// Burada kullanıcının Shipped öncesi siparişini güvenli provider reversal sonucu ile iptal etme isteğini taşıyorum.
public sealed record CancelOrderCommand(Guid OrderId) : IRequest<OrderCancellationResult>;
