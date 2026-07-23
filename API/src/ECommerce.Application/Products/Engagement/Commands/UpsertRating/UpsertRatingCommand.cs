using MediatR;

namespace ECommerce.Application.Products.Engagement.Commands.UpsertRating;

public sealed record UpsertRatingCommand(long ProductId, int RatingValue) : IRequest;
