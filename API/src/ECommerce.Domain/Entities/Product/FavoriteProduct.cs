using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class FavoriteProduct : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private FavoriteProduct()
    {
    }

    public FavoriteProduct(Guid productId, Guid userId)
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
        CreatedAt = DateTime.UtcNow;
    }
}
