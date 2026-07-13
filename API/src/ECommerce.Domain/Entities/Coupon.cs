using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class Coupon : AuditableEntity
{
    public string Code { get; private set; } = null!;
    public string? Description { get; private set; }
    public CouponDiscountType DiscountType { get; private set; }
    public decimal DiscountValue { get; private set; }
    public decimal? MinimumOrderAmount { get; private set; }
    public int? UsageLimit { get; private set; }
    public int UsedCount { get; private set; }
    public DateTime? StartsAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool IsActive { get; private set; }

    private Coupon()
    {
    }

    public Coupon(
        string code,
        CouponDiscountType discountType,
        decimal discountValue,
        string? description = null,
        decimal? minimumOrderAmount = null,
        int? usageLimit = null,
        DateTime? startsAt = null,
        DateTime? expiresAt = null,
        bool isActive = true)
    {
        SetCode(code);
        DiscountType = discountType;
        ValidateDiscountValue(discountValue, discountType);
        ValidateUsageLimit(usageLimit);
        ValidateMinimumOrderAmount(minimumOrderAmount);
        ValidateDates(startsAt, expiresAt);

        DiscountValue = discountValue;
        UsageLimit = usageLimit;
        MinimumOrderAmount = minimumOrderAmount;
        Description = description?.Trim();
        StartsAt = startsAt;
        ExpiresAt = expiresAt;
        IsActive = isActive;
    }

    public void IncreaseUsedCount()
    {
        if (UsageLimit.HasValue && UsedCount >= UsageLimit.Value)
        {
            throw new DomainException("Coupon usage limit reached.");
        }

        UsedCount++;
        MarkAsUpdated();
    }

    public void DecreaseUsedCount()
    {
        if (UsedCount <= 0)
        {
            throw new DomainException("Used count cannot become negative.");
        }

        UsedCount--;
        MarkAsUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    private void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Coupon code cannot be empty.");
        }

        Code = code.Trim();
    }

    private static void ValidateDiscountValue(decimal discountValue, CouponDiscountType discountType)
    {
        if (discountValue <= 0)
        {
            throw new DomainException("Discount value must be greater than zero.");
        }

        if (discountType == CouponDiscountType.Percentage && (discountValue <= 0 || discountValue > 100))
        {
            throw new DomainException("Percentage discount must be between 0 and 100.");
        }
    }

    private static void ValidateUsageLimit(int? usageLimit)
    {
        if (usageLimit.HasValue && usageLimit.Value <= 0)
        {
            throw new DomainException("Usage limit must be greater than zero.");
        }
    }

    private static void ValidateMinimumOrderAmount(decimal? minimumOrderAmount)
    {
        if (minimumOrderAmount.HasValue && minimumOrderAmount.Value < 0)
        {
            throw new DomainException("Minimum order amount cannot be negative.");
        }
    }

    private static void ValidateDates(DateTime? startsAt, DateTime? expiresAt)
    {
        if (startsAt.HasValue && expiresAt.HasValue && startsAt.Value > expiresAt.Value)
        {
            throw new DomainException("Coupon start date cannot be later than expiry date.");
        }
    }
}
