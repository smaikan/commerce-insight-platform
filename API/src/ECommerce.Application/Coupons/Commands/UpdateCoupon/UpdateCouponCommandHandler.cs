using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Coupons.Dtos;
using MediatR;

namespace ECommerce.Application.Coupons.Commands.UpdateCoupon;

public sealed class UpdateCouponCommandHandler : IRequestHandler<UpdateCouponCommand, CouponDto>
{
    private readonly ICouponRepository _couponRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada kupon gÃ¼ncelleme use-case'i iÃ§in repository ve kayÄ±t koordinasyon baÄŸÄ±mlÄ±lÄ±klarÄ±nÄ± hazÄ±rlÄ±yorum.
    public UpdateCouponCommandHandler(ICouponRepository couponRepository, IUnitOfWork unitOfWork)
    {
        _couponRepository = couponRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada hedef kuponu, kod Ã§akÄ±ÅŸmasÄ±nÄ± ve kullanÄ±m limiti dahil domain kurallarÄ±nÄ± denetleyerek gÃ¼ncelliyorum.
    public async Task<CouponDto> Handle(UpdateCouponCommand request, CancellationToken cancellationToken)
    {
        var coupon = await _couponRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (coupon is null)
        {
            throw new NotFoundException("Coupon was not found.");
        }

        if (await _couponRepository.CodeExistsAsync(request.Code, request.Id, cancellationToken))
        {
            throw new ConflictException("Coupon code already exists.");
        }

        coupon.Update(
            request.Code,
            request.DiscountType,
            request.DiscountValue,
            request.Description,
            request.MinimumOrderAmount,
            request.UsageLimit,
            request.StartsAt,
            request.ExpiresAt);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return coupon.ToDto();
    }
}
