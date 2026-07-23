using MediatR;

namespace ECommerce.Application.Products.Engagement.Commands.RemoveFavorite;

public sealed record RemoveFavoriteCommand(long ProductId) : IRequest;
