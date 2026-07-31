using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Coupons.Dtos;
using MediatR;

namespace ECommerce.Application.Coupons.Queries.GetCoupons;

public sealed class GetCouponsQueryHandler : IRequestHandler<GetCouponsQuery, PagedResult<CouponDto>>
{
    private readonly ICouponRepository _couponRepository;

    // Burada kupon listeleme use-case'i iÃ§in repository baÄŸÄ±mlÄ±lÄ±ÄŸÄ±nÄ± hazÄ±rlÄ±yorum.
    public GetCouponsQueryHandler(ICouponRepository couponRepository)
    {
        _couponRepository = couponRepository;
    }

    // Burada kupon sayfasÄ±nÄ± filtreyle okuyup DTO modeline dÃ¶nÃ¼ÅŸtÃ¼rÃ¼yorum.
    public async Task<PagedResult<CouponDto>> Handle(GetCouponsQuery request, CancellationToken cancellationToken)
    {
        var coupons = await _couponRepository.GetListAsync(
            request.PageNumber,
            request.PageSize,
            request.IsActive,
            cancellationToken);
        return coupons.Map(coupon => coupon.ToDto());
    }
}
