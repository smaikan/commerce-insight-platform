using ECommerce.Application.Orders.Dtos;
using MediatR;

namespace ECommerce.Application.Orders.Commands.CreateOrder;

// Burada güncel kullanıcı sepetinden sipariş oluşturma isteğini taşıyorum.
public sealed record CreateOrderCommand(
    Guid ExpectedCartConcurrencyToken,
    Guid? ShippingAddressId = null,
    string? CouponCode = null,
    Guid? ShippingMethodId = null) : IRequest<OrderDto>;
