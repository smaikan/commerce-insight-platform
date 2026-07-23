using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class FavoriteProduct : BaseEntity
{
    public long ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public long UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private FavoriteProduct()
    {
    }

    public FavoriteProduct(long productId, long userId)
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
        CreatedAt = DateTime.UtcNow;
    }
}
