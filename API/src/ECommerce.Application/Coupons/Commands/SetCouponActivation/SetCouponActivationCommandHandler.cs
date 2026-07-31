using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Coupons.Dtos;
using MediatR;

namespace ECommerce.Application.Coupons.Commands.SetCouponActivation;

public sealed class SetCouponActivationCommandHandler : IRequestHandler<SetCouponActivationCommand, CouponDto>
{
    private readonly ICouponRepository _couponRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada kupon aktiflik use-case'i iÃ§in repository ve kayÄ±t koordinasyon baÄŸÄ±mlÄ±lÄ±klarÄ±nÄ± hazÄ±rlÄ±yorum.
    public SetCouponActivationCommandHandler(ICouponRepository couponRepository, IUnitOfWork unitOfWork)
    {
        _couponRepository = couponRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada bulunan kuponu istenen aktiflik durumuna getirip kaydediyorum.
    public async Task<CouponDto> Handle(SetCouponActivationCommand request, CancellationToken cancellationToken)
    {
        var coupon = await _couponRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (coupon is null)
        {
            throw new NotFoundException("Coupon was not found.");
        }

        if (request.IsActive)
        {
            coupon.Activate();
        }
        else
        {
            coupon.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return coupon.ToDto();
    }
}
