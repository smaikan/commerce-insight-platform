using MediatR;

namespace ECommerce.Application.Products.Engagement.Commands.AddFavorite;

// Burada favoriye eklenecek ürünü ve anonim sahiplik için isteğe bağlı session değerini taşıyorum.
public sealed record AddFavoriteCommand(long ProductId, string? SessionId = null) : IRequest;
