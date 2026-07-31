using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class CouponUsage : BaseEntity
{
    public Guid CouponId { get; private set; }
    public Coupon Coupon { get; private set; } = null!;
    public long UserId { get; private set; }
    public Guid? OrderId { get; private set; }
    public DateTime UsedAt { get; private set; }

    // Burada EF Core'un kupon kullanÄ±m kaydÄ±nÄ± veritabanÄ±ndan oluÅŸturabilmesi iÃ§in boÅŸ kurucuyu tutuyorum.
    private CouponUsage()
    {
    }

    // Burada kupon, kullanÄ±cÄ±, isteÄŸe baÄŸlÄ± sipariÅŸ ve UTC kullanÄ±m zamanÄ±yla kullanÄ±m kaydÄ±nÄ± oluÅŸturuyorum.
    public CouponUsage(
        Guid couponId,
        long userId,
        Guid? orderId = null,
        DateTime? usedAt = null)
    {
        if (couponId == Guid.Empty || userId <= 0)
        {
            throw new DomainException("Coupon id and user id are required.");
        }

        if (orderId == Guid.Empty)
        {
            throw new DomainException("Order id cannot be empty.");
        }

        var effectiveUsedAt = usedAt ?? DateTime.UtcNow;
        if (effectiveUsedAt.Kind != DateTimeKind.Utc)
        {
            throw new DomainException("Coupon usage time must be UTC.");
        }

        CouponId = couponId;
        UserId = userId;
        OrderId = orderId;
        UsedAt = effectiveUsedAt;
    }

    // Burada henÃ¼z sipariÅŸe baÄŸlanmamÄ±ÅŸ kullanÄ±m kaydÄ±nÄ± tek bir sipariÅŸe geri dÃ¶nÃ¼ÅŸsÃ¼z baÄŸlÄ±yorum.
    public void AssignToOrder(Guid orderId)
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainException("Order id cannot be empty.");
        }

        if (OrderId.HasValue && OrderId.Value != orderId)
        {
            throw new DomainException("Coupon usage is already assigned to another order.");
        }

        OrderId = orderId;
    }
}
