using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class Cart : BaseEntity
{
    public Guid? UserId { get; private set; }
    public string? SessionId { get; private set; }
    public ICollection<CartItem> Items { get; private set; } = new List<CartItem>();
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Cart()
    {
    }

    public Cart(Guid? userId = null, string? sessionId = null)
    {
        if (userId is null && string.IsNullOrWhiteSpace(sessionId))
        {
            throw new DomainException("Cart must have either user id or session id.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException("User id cannot be empty.");
        }

        UserId = userId;
        SessionId = sessionId?.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
