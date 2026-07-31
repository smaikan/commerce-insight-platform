using ECommerce.Application.Carts.Dtos;
using MediatR;

namespace ECommerce.Application.Carts.Commands.ClearCart;

// Burada owner'a ait sepeti concurrency korumasıyla temizleme isteğini taşıyorum.
public sealed record ClearCartCommand(
    Guid ExpectedConcurrencyToken,
    string? SessionId = null) : IRequest<CartDto>;
