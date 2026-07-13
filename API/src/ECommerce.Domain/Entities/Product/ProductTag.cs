using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ProductTag : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public Guid TagId { get; private set; }
    public Tag Tag { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private ProductTag()
    {
    }

    public ProductTag(Guid productId, Guid tagId)
    {
        if (productId == Guid.Empty)
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
}
