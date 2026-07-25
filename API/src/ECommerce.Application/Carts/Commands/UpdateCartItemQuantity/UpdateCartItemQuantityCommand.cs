using ECommerce.Application.Carts.Dtos;
using MediatR;

namespace ECommerce.Application.Carts.Commands.UpdateCartItemQuantity;

// Burada sepet satırının adedini güncel fiyat ve concurrency tokenıyla değiştirme isteğini taşıyorum.
public sealed record UpdateCartItemQuantityCommand(
    Guid CartItemId,
    int Quantity,
    Guid ExpectedConcurrencyToken,
    string? SessionId = null) : IRequest<CartDto>;
