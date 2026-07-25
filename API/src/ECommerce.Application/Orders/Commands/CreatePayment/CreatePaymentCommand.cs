using ECommerce.Application.Orders.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Orders.Commands.CreatePayment;

// Burada kullanıcıya ait sipariş için idempotent ödeme denemesi oluşturma isteğini taşıyorum.
public sealed record CreatePaymentCommand(
    Guid OrderId,
    PaymentProvider Provider,
    string IdempotencyKey) : IRequest<PaymentDto>;
