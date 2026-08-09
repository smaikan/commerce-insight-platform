using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

// Burada guest oturumunun erişebildiği tek siparişi açık bir yetki kaydıyla bağlıyorum.
public sealed class GuestOrderAccessGrant : BaseEntity
{
    public Guid SessionId { get; private set; }
    public GuestOrderSession Session { get; private set; } = null!;
    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;
    public DateTime GrantedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    // Burada EF Core'un guest erişim kaydını oluşturabilmesi için boş kurucuyu tutuyorum.
    private GuestOrderAccessGrant()
    {
    }

    // Burada geçerli oturum ve sipariş için guest erişim yetkisi oluşturuyorum.
    public GuestOrderAccessGrant(Guid sessionId, Guid orderId, DateTime grantedAt)
    {
        if (sessionId == Guid.Empty || orderId == Guid.Empty || grantedAt.Kind != DateTimeKind.Utc)
        {
            throw new DomainException("Guest access grant values are invalid.");
        }

        SessionId = sessionId;
        OrderId = orderId;
        GrantedAt = grantedAt;
    }

    // Burada claim sonrasında guest sipariş erişim yetkisini iptal ediyorum.
    public void Revoke(DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new DomainException("Guest access revocation time must be UTC.");
        }

        RevokedAt ??= utcNow;
    }

    // Burada daha önce iptal edilmiş aynı session-sipariş yetkisini yeni doğrulanmış magic-link ile yeniden etkinleştiriyorum.
    public void Reactivate(DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new DomainException("Guest access grant time must be UTC.");
        }

        GrantedAt = utcNow;
        RevokedAt = null;
    }
}
