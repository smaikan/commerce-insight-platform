using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ProductReview : BaseEntity
{
    public long ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public long UserId { get; private set; }
    public string? Title { get; private set; }
    public string Comment { get; private set; } = null!;
    public int? RatingValue { get; private set; }
    public bool IsApproved { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ProductReview()
    {
    }

    public ProductReview(long productId, long userId, string comment, string? title = null, int? ratingValue = null, bool isApproved = false)
    {
        if (productId <= 0)
        {
            throw new DomainException("Product id is required.");
        }

        if (userId <= 0)
        {
            throw new DomainException("User id is required.");
        }

        ProductId = productId;
        UserId = userId;
        SetComment(comment);
        SetRatingValue(ratingValue);
        Title = title?.Trim();
        IsApproved = isApproved;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string comment, string? title, int? ratingValue)
    {
        SetComment(comment);
        SetRatingValue(ratingValue);
        Title = title?.Trim();
    }

    public void Approve()
    {
        IsApproved = true;
    }

    public void Reject()
    {
        IsApproved = false;
    }

    private void SetComment(string comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new DomainException("Review comment cannot be empty.");
        }

        Comment = comment.Trim();
    }

    private void SetRatingValue(int? ratingValue)
    {
        if (ratingValue.HasValue && (ratingValue.Value < 1 || ratingValue.Value > 5))
        {
            throw new DomainException("Review rating value must be between 1 and 5.");
        }

        RatingValue = ratingValue;
    }
}
