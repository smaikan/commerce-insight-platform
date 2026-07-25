using ECommerce.Application.Coupons.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Coupons.Commands.CreateCoupon;

// Burada yÃ¶neticinin yeni kupon oluÅŸturma isteÄŸini taÅŸÄ±yorum.
public sealed record CreateCouponCommand(
    string Code,
    CouponDiscountType DiscountType,
    decimal DiscountValue,
    string? Description = null,
    decimal? MinimumOrderAmount = null,
    int? UsageLimit = null,
    DateTime? StartsAt = null,
    DateTime? ExpiresAt = null,
    bool IsActive = true) : IRequest<CouponDto>;
