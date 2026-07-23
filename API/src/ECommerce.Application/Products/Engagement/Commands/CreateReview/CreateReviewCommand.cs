using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Engagement.Commands.CreateReview;

public sealed record CreateReviewCommand(long ProductId, string Comment, string? Title = null, int? RatingValue = null)
    : IRequest<ProductReviewDto>;
