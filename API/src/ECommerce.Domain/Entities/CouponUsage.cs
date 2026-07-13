using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class CouponUsage : BaseEntity
{
    public Guid CouponId { get; private set; }
    public Coupon Coupon { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public Guid? OrderId { get; private set; }
    public DateTime UsedAt { get; private set; }

    private CouponUsage()
    {
    }

    public CouponUsage(Guid couponId, Guid userId, Guid? orderId = null)
    {
        if (couponId == Guid.Empty || userId == Guid.Empty)
        {
            throw new DomainException("Coupon id and user id are required.");
        }

        if (orderId == Guid.Empty)
        {
            throw new DomainException("Order id cannot be empty.");
        }

        CouponId = couponId;
        UserId = userId;
        OrderId = orderId;
        UsedAt = DateTime.UtcNow;
    }
}
