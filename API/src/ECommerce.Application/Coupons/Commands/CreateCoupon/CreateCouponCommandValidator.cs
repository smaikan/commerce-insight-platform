using ECommerce.Domain.Entities;
using FluentValidation;

namespace ECommerce.Application.Coupons.Commands.CreateCoupon;

public sealed class CreateCouponCommandValidator : AbstractValidator<CreateCouponCommand>
{
    // Burada yeni kupon isteÄŸinin temel metin, enum, tutar, limit ve tarih alanlarÄ±nÄ± doÄŸruluyorum.
    public CreateCouponCommandValidator()
    {
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
