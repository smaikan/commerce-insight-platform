using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ProductBundleItem : BaseEntity
{
    public long BundleProductId { get; private set; }
    public Product BundleProduct { get; private set; } = null!;
    public long IncludedProductId { get; private set; }
    public Product IncludedProduct { get; private set; } = null!;
    public int Quantity { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ProductBundleItem()
    {
    }

    public ProductBundleItem(long bundleProductId, long includedProductId, int quantity)
    {
        if (bundleProductId <= 0 || includedProductId <= 0)
        {
            throw new DomainException("Bundle and included product ids are required.");
        }

        if (bundleProductId == includedProductId)
        {
            throw new DomainException("Bundle product and included product cannot be the same.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        BundleProductId = bundleProductId;
        IncludedProductId = includedProductId;
        Quantity = quantity;
        CreatedAt = DateTime.UtcNow;
    }

    public void ChangeQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        Quantity = quantity;
    }
}
