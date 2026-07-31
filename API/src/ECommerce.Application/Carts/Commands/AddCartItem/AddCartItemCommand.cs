using ECommerce.Application.Carts.Dtos;
using MediatR;

namespace ECommerce.Application.Carts.Commands.AddCartItem;

// Burada güncel veritabanı fiyatıyla sepete varyant ekleme isteğini taşıyorum.
public sealed record AddCartItemCommand(
    Guid ProductVariantId,
    int Quantity,
    string? SessionId = null,
    Guid? ExpectedConcurrencyToken = null) : IRequest<CartDto>;
