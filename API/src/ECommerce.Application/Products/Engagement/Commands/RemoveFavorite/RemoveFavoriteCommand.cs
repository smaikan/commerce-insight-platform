using MediatR;

namespace ECommerce.Application.Products.Engagement.Commands.RemoveFavorite;

// Burada favoriden çıkarılacak ürünü ve anonim sahiplik için isteğe bağlı session değerini taşıyorum.
public sealed record RemoveFavoriteCommand(long ProductId, string? SessionId = null) : IRequest;
