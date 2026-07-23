using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ProductTag : BaseEntity
{
    public long ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public Guid TagId { get; private set; }
    public Tag Tag { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private ProductTag()
    {
    }

    public ProductTag(long productId, Guid tagId)
    {
        if (productId <= 0)
        {
            throw new DomainException("Product id cannot be empty.");
        }

        if (tagId == Guid.Empty)
        {
            throw new DomainException("Tag id cannot be empty.");
        }

        ProductId = productId;
        TagId = tagId;
        CreatedAt = DateTime.UtcNow;
    }

    public ProductTag(Product product, Guid tagId)
        : this(1, tagId)
    {
        Product = product ?? throw new DomainException("Product cannot be empty.");
        ProductId = product.Id;
    }
}
