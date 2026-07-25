using ECommerce.Application.Coupons.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Coupons.Commands.UpdateCoupon;

// Burada yÃ¶neticinin mevcut kuponun uygulanabilirlik ayarlarÄ±nÄ± gÃ¼ncelleme isteÄŸini taÅŸÄ±yorum.
public sealed record UpdateCouponCommand(
    Guid Id,
    string Code,
    CouponDiscountType DiscountType,
    decimal DiscountValue,
    string? Description = null,
    decimal? MinimumOrderAmount = null,
    int? UsageLimit = null,
    DateTime? StartsAt = null,
    DateTime? ExpiresAt = null) : IRequest<CouponDto>;
