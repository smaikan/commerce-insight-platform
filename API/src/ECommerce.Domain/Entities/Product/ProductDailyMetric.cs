using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ProductDailyMetric : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public DateOnly Date { get; private set; }
    public int ClickCount { get; private set; }
    public int AddToCartCount { get; private set; }
    public int PurchaseCount { get; private set; }
    public int FavoriteCount { get; private set; }
    public int RatingCount { get; private set; }
    public int ReviewCount { get; private set; }

    private ProductDailyMetric()
    {
    }

    public ProductDailyMetric(Guid productId, DateOnly date)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product id is required.");
        }

        ProductId = productId;
        Date = date;
    }

    public void IncreaseClickCount()
    {
        ClickCount++;
    }

    public void IncreaseAddToCartCount(int quantity)
    {
        ValidateQuantity(quantity);
        AddToCartCount += quantity;
    }

    public void IncreasePurchaseCount(int quantity)
    {
        ValidateQuantity(quantity);
        PurchaseCount += quantity;
    }

    public void IncreaseFavoriteCount()
    {
        FavoriteCount++;
    }

    public void IncreaseRatingCount()
    {
        RatingCount++;
    }

    public void IncreaseReviewCount()
    {
        ReviewCount++;
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }
    }
}
