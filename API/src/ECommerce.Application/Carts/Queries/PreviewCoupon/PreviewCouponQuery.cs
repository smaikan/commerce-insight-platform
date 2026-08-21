using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Carts.Queries.PreviewCoupon;

public sealed record PreviewCouponQuery(
    string CouponCode,
    bool IsGuest,
    string? SessionId = null) : IRequest<CouponPreviewDto>;

public sealed record CouponPreviewDto(
    string Code,
    decimal DiscountTotal,
    CouponDiscountType DiscountType);
