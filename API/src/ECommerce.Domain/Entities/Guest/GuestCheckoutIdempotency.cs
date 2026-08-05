using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

// Burada guest checkout tekrarlarının aynı sipariş sonucuna güvenle bağlanmasını sağlıyorum.
public sealed class GuestCheckoutIdempotency : BaseEntity
{
    public string CartSessionHash { get; private set; } = null!;
    public string KeyHash { get; private set; } = null!;
    public string RequestHash { get; private set; } = null!;
    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }

    // Burada EF Core'un idempotency kaydını oluşturabilmesi için boş kurucuyu tutuyorum.
    private GuestCheckoutIdempotency()
    {
    }

    // Burada aynı guest cart ve anahtar için değişmez checkout sonucu oluşturuyorum.
    public GuestCheckoutIdempotency(
        string cartSessionHash,
        string keyHash,
        string requestHash,
        Order order,
        DateTime utcNow,
        DateTime expiresAt)
    {
        if (order is null || order.Id == Guid.Empty || utcNow.Kind != DateTimeKind.Utc || expiresAt.Kind != DateTimeKind.Utc || expiresAt <= utcNow)
        {
            throw new DomainException("Guest checkout idempotency values are invalid.");
        }

        CartSessionHash = GuestOrderSession.NormalizeHash(cartSessionHash, "Cart session hash");
        KeyHash = GuestOrderSession.NormalizeHash(keyHash, "Idempotency key hash");
        RequestHash = GuestOrderSession.NormalizeHash(requestHash, "Checkout request hash");
        OrderId = order.Id;
        Order = order;
        ExpiresAt = expiresAt;
    }

    // Burada süresi dolmuş anahtarın yeni checkout intent sonucuna güvenli biçimde yeniden bağlanmasını sağlıyorum.
    public void ReplaceExpiredResult(string requestHash, Order order, DateTime utcNow, DateTime expiresAt)
    {
        if (ExpiresAt > utcNow || order is null || order.Id == Guid.Empty || utcNow.Kind != DateTimeKind.Utc ||
            expiresAt.Kind != DateTimeKind.Utc || expiresAt <= utcNow)
        {
            throw new DomainException("Only an expired idempotency result can be replaced.");
        }

        RequestHash = GuestOrderSession.NormalizeHash(requestHash, "Checkout request hash");
        OrderId = order.Id;
        Order = order;
        ExpiresAt = expiresAt;
    }
}
