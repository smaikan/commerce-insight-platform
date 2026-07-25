using ECommerce.Application.Carts.Dtos;
using MediatR;

namespace ECommerce.Application.Carts.Commands.RemoveCartItem;

// Burada owner'a ait sepet satırını concurrency korumasıyla kaldırma isteğini taşıyorum.
public sealed record RemoveCartItemCommand(
    Guid CartItemId,
    Guid ExpectedConcurrencyToken,
    string? SessionId = null) : IRequest<CartDto>;
