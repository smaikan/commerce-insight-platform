using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Coupons.Dtos;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Coupons.Commands.CreateCoupon;

public sealed class CreateCouponCommandHandler : IRequestHandler<CreateCouponCommand, CouponDto>
{
    private readonly ICouponRepository _couponRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada kupon oluÅŸturma use-case'i iÃ§in repository ve kayÄ±t koordinasyon baÄŸÄ±mlÄ±lÄ±klarÄ±nÄ± hazÄ±rlÄ±yorum.
    public CreateCouponCommandHandler(ICouponRepository couponRepository, IUnitOfWork unitOfWork)
    {
        _couponRepository = couponRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada kod Ã§akÄ±ÅŸmasÄ±nÄ± denetleyip yeni kuponu kalÄ±cÄ± olarak oluÅŸturuyorum.
    public async Task<CouponDto> Handle(CreateCouponCommand request, CancellationToken cancellationToken)
    {
        if (await _couponRepository.CodeExistsAsync(request.Code, cancellationToken: cancellationToken))
        {
            throw new ConflictException("Coupon code already exists.");
        }

        var coupon = new Coupon(
            request.Code,
            request.DiscountType,
            request.DiscountValue,
            request.Description,
            request.MinimumOrderAmount,
            request.UsageLimit,
            request.StartsAt,
            request.ExpiresAt,
            request.IsActive,
            request.IsMemberOnly);

        await _couponRepository.AddAsync(coupon, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return coupon.ToDto();
    }
}
