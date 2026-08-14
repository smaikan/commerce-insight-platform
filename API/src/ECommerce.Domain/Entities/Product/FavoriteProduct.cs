using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class FavoriteProduct : BaseEntity
{
    public const int MaximumSessionIdLength = 120;

    public long ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public long? UserId { get; private set; }
    public string? SessionId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Burada EF Core'un favori kaydını veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private FavoriteProduct()
    {
    }

    // Burada kayıtlı kullanıcıya ait favoriyi tek sahip invariant'ıyla oluşturuyorum.
    public FavoriteProduct(long productId, long userId)
        : this(productId, userId, null)
    {
    }

    // Burada misafir oturumuna ait favoriyi tek sahip invariant'ıyla oluşturuyorum.
    public FavoriteProduct(long productId, string sessionId)
        : this(productId, null, sessionId)
    {
    }

    // Burada favorinin yalnız bir kullanıcıya veya bir misafir oturumuna ait olmasını sağlıyorum.
    private FavoriteProduct(long productId, long? userId, string? sessionId)
    {
        if (productId <= 0)
        {
            throw new DomainException("Product id is required.");
        }

        if (userId.HasValue == !string.IsNullOrWhiteSpace(sessionId))
        {
            throw new DomainException("Favorite must belong to either a user or a guest session.");
        }

        if (userId is <= 0)
        {
            throw new DomainException("User id is required.");
        }

        var normalizedSessionId = sessionId?.Trim();
        if (normalizedSessionId?.Length > MaximumSessionIdLength)
        {
            throw new DomainException($"Session id cannot exceed {MaximumSessionIdLength} characters.");
        }

        ProductId = productId;
        UserId = userId;
        SessionId = normalizedSessionId;
        CreatedAt = DateTime.UtcNow;
    }

    // Burada misafir favorisini kayıtlı kullanıcıya devredip session bağlantısını kaldırıyorum.
    public void AssignToUser(long userId)
    {
        if (userId <= 0)
        {
            throw new DomainException("User id is required.");
        }

        if (UserId == userId)
        {
            return;
        }

        if (UserId.HasValue)
        {
            throw new DomainException("A registered favorite cannot be assigned to another user.");
        }

        UserId = userId;
        SessionId = null;
    }
}
