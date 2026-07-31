using FluentValidation;

namespace ECommerce.Application.Coupons.Commands.SetCouponActivation;

public sealed class SetCouponActivationCommandValidator : AbstractValidator<SetCouponActivationCommand>
{
    // Burada aktiflik deÄŸiÅŸikliÄŸinin hedef kupon kimliÄŸini doÄŸruluyorum.
    public SetCouponActivationCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();
    }
}
