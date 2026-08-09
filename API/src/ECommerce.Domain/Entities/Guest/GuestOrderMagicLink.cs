using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

// Burada guest sipariş magic-link tokenını yalnız hash olarak ve tek kullanımlık yaşam döngüsüyle saklıyorum.
public sealed class GuestOrderMagicLink : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;
    public string TokenHash { get; private set; } = null!;
    public string EmailHash { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    // Burada EF Core'un magic-link kaydını oluşturabilmesi için boş kurucuyu tutuyorum.
    private GuestOrderMagicLink()
    {
    }

    // Burada siparişe bağlı süreli ve hash'lenmiş magic-link kaydını oluşturuyorum.
    public GuestOrderMagicLink(Guid orderId, string tokenHash, string emailHash, DateTime utcNow, DateTime expiresAt)
    {
        if (orderId == Guid.Empty || utcNow.Kind != DateTimeKind.Utc || expiresAt.Kind != DateTimeKind.Utc || expiresAt <= utcNow)
        {
            throw new DomainException("Guest magic-link values are invalid.");
        }

        OrderId = orderId;
        TokenHash = GuestOrderSession.NormalizeHash(tokenHash, "Magic-link token hash");
        EmailHash = GuestOrderSession.NormalizeHash(emailHash, "Magic-link email hash");
        CreatedAt = utcNow;
        ExpiresAt = expiresAt;
    }

    // Burada magic linki yalnız aktifken tek kez tüketiyorum.
    public void Consume(DateTime utcNow)
    {
        if (!IsActiveAt(utcNow))
        {
            throw new DomainException("Guest magic link is expired or already used.");
        }

        UsedAt = utcNow;
    }

    // Burada eski veya claim edilmiş magic linki iptal ediyorum.
    public void Revoke(DateTime utcNow)
    {
        EnsureUtc(utcNow);
        RevokedAt ??= utcNow;
    }

    // Burada magic linkin belirtilen anda kullanılabilir olmasını denetliyorum.
    public bool IsActiveAt(DateTime utcNow)
    {
        EnsureUtc(utcNow);
        return !UsedAt.HasValue && !RevokedAt.HasValue && ExpiresAt > utcNow;
    }

    // Burada magic-link zamanlarının UTC olmasını zorunlu tutuyorum.
    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new DomainException("Guest magic-link time must be UTC.");
        }
    }
}
