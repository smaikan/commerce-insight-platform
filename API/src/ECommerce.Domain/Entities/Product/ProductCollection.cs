using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ProductCollection : BaseEntity
{
    public long ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public Guid CollectionId { get; private set; }
    public Collection Collection { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private ProductCollection()
    {
    }

    public ProductCollection(long productId, Guid collectionId)
    {
        if (productId <= 0)
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

    public ProductCollection(Product product, Guid collectionId)
        : this(1, collectionId)
    {
        Product = product ?? throw new DomainException("Product cannot be empty.");
        ProductId = product.Id;
    }
}
