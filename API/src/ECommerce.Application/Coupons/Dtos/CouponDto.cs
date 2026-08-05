using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Coupons.Dtos;

// Burada kuponun yÃ¶netim ve listeleme ekranlarÄ±nda kullanÄ±lan gÃ¼venli cevap modelini tanÄ±mlÄ±yorum.
public sealed record CouponDto(
    Guid Id,
    string Code,
    string? Description,
    CouponDiscountType DiscountType,
    decimal DiscountValue,
    decimal? MinimumOrderAmount,
    int? UsageLimit,
    int UsedCount,
    DateTime? StartsAt,
    DateTime? ExpiresAt,
    bool IsActive,
    bool IsMemberOnly,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public static class CouponDtoMapping
{
    // Burada domain kuponunu API katmanÄ±na taÅŸÄ±nabilecek DTO modeline dÃ¶nÃ¼ÅŸtÃ¼rÃ¼yorum.
    public static CouponDto ToDto(this Coupon coupon)
    {
        return new CouponDto(
            coupon.Id,
            coupon.Code,
            coupon.Description,
            coupon.DiscountType,
            coupon.DiscountValue,
            coupon.MinimumOrderAmount,
            coupon.UsageLimit,
            coupon.UsedCount,
            coupon.StartsAt,
            coupon.ExpiresAt,
            coupon.IsActive,
            coupon.IsMemberOnly,
            coupon.CreatedAt,
            coupon.UpdatedAt);
    }
}
