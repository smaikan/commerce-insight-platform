using ECommerce.Domain.Entities;
using ECommerce.Application.Common.Identifiers;

namespace ECommerce.Application.Products.Dtos;

public sealed record ProductReviewDto(
    Guid Id,
    string ProductId,
    string UserId,
    string? Title,
    string Comment,
    int? RatingValue,
    bool IsApproved,
    DateTime CreatedAt);

public static class ProductReviewDtoMapping
{
    public static ProductReviewDto ToDto(this ProductReview review) => new(
        review.Id, PublicIdCodec.EncodeProductId(review.ProductId), PublicIdCodec.EncodeUserId(review.UserId), review.Title, review.Comment,
        review.RatingValue, review.IsApproved, review.CreatedAt);
}
