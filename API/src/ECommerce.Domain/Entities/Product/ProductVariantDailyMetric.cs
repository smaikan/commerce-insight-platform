using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ProductVariantDailyMetric : BaseEntity
{
    public Guid ProductVariantId { get; private set; }
    public ProductVariant ProductVariant { get; private set; } = null!;
    public DateOnly Date { get; private set; }
    public int AddToCartCount { get; private set; }
    public int PurchaseCount { get; private set; }

    private ProductVariantDailyMetric()
    {
    }

    public ProductVariantDailyMetric(Guid productVariantId, DateOnly date)
    {
        if (productVariantId == Guid.Empty)
        {
            throw new DomainException("Product variant id is required.");
        }

        ProductVariantId = productVariantId;
        Date = date;
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

    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }
    }
}
