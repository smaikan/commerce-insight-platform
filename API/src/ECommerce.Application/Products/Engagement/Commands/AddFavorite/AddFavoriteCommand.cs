using MediatR;

namespace ECommerce.Application.Products.Engagement.Commands.AddFavorite;

public sealed record AddFavoriteCommand(long ProductId) : IRequest;
