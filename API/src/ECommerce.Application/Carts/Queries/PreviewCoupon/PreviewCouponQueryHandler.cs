using ECommerce.Application.Carts.Queries.GetCart;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Orders.Services;
using MediatR;

namespace ECommerce.Application.Carts.Queries.PreviewCoupon;

public sealed class PreviewCouponQueryHandler : IRequestHandler<PreviewCouponQuery, CouponPreviewDto>
{
    private readonly ISender _sender;
    private readonly OrderCouponService _couponService;

    public PreviewCouponQueryHandler(
        ISender sender,
        OrderCouponService couponService)
    {
        _sender = sender;
        _couponService = couponService;
    }

    public async Task<CouponPreviewDto> Handle(
        PreviewCouponQuery request,
        CancellationToken cancellationToken)
    {
        var cartDto = await _sender.Send(new GetCartQuery(request.SessionId), cancellationToken);

        if (cartDto.Items.Count == 0)
        {
            throw new ConflictException("Cart is empty.");
        }

        var checkoutCoupon = await _couponService.ResolveForCheckoutAsync(
            request.CouponCode,
            cartDto.SubTotal,
            request.IsGuest,
            cancellationToken);

        if (checkoutCoupon is null)
        {
            throw new NotFoundException("Coupon was not found.");
        }

        return new CouponPreviewDto(
            checkoutCoupon.Coupon.Code,
            checkoutCoupon.DiscountTotal,
            checkoutCoupon.Coupon.DiscountType);
    }
}
