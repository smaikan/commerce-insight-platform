using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ProductRating : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public int RatingValue { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ProductRating()
    {
    }

    public ProductRating(Guid productId, Guid userId, int ratingValue)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product id is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        ProductId = productId;
        UserId = userId;
        SetRatingValue(ratingValue);
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateRatingValue(int ratingValue)
    {
        SetRatingValue(ratingValue);
    }

    private void SetRatingValue(int ratingValue)
    {
        if (ratingValue < 1 || ratingValue > 5)
        {
            throw new DomainException("Rating value must be between 1 and 5.");
        }

        RatingValue = ratingValue;
    }
}
