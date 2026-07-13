using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ProductCollection : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public Guid CollectionId { get; private set; }
    public Collection Collection { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private ProductCollection()
    {
    }

    public ProductCollection(Guid productId, Guid collectionId)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product id cannot be empty.");
        }

        if (collectionId == Guid.Empty)
        {
            throw new DomainException("Collection id cannot be empty.");
        }

        ProductId = productId;
        CollectionId = collectionId;
        CreatedAt = DateTime.UtcNow;
    }
}
