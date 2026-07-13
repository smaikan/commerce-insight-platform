using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ProductBundleItem : BaseEntity
{
    public Guid BundleProductId { get; private set; }
    public Product BundleProduct { get; private set; } = null!;
    public Guid IncludedProductId { get; private set; }
    public Product IncludedProduct { get; private set; } = null!;
    public int Quantity { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ProductBundleItem()
    {
    }

    public ProductBundleItem(Guid bundleProductId, Guid includedProductId, int quantity)
    {
        if (bundleProductId == Guid.Empty || includedProductId == Guid.Empty)
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
