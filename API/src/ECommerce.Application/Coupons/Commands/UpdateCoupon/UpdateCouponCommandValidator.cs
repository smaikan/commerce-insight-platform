using ECommerce.Domain.Entities;
using FluentValidation;

namespace ECommerce.Application.Coupons.Commands.UpdateCoupon;

public sealed class UpdateCouponCommandValidator : AbstractValidator<UpdateCouponCommand>
{
    // Burada kupon gÃ¼ncelleme isteÄŸinin kimlik, indirim, limit ve tarih alanlarÄ±nÄ± doÄŸruluyorum.
    public UpdateCouponCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Code)
            .NotEmpty()
            .MaximumLength(Coupon.MaximumCodeLength)
            .Matches(Coupon.CodePattern);

        RuleFor(command => command.DiscountType)
            .IsInEnum();

        RuleFor(command => command.DiscountValue)
            .GreaterThan(0m);

        RuleFor(command => command.Description)
            .MaximumLength(Coupon.MaximumDescriptionLength);

        RuleFor(command => command.MinimumOrderAmount!.Value)
            .GreaterThanOrEqualTo(0m)
            .When(command => command.MinimumOrderAmount.HasValue);

        RuleFor(command => command.UsageLimit!.Value)
            .GreaterThan(0)
            .When(command => command.UsageLimit.HasValue);

        RuleFor(command => command.ExpiresAt)
            .GreaterThanOrEqualTo(command => command.StartsAt!.Value)
            .When(command => command.StartsAt.HasValue && command.ExpiresAt.HasValue);
    }
}
