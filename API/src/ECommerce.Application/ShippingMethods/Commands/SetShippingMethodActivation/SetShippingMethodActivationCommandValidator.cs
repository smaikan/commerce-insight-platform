using FluentValidation;

namespace ECommerce.Application.ShippingMethods.Commands.SetShippingMethodActivation;

public sealed class SetShippingMethodActivationCommandValidator : AbstractValidator<SetShippingMethodActivationCommand>
{
    // Burada aktiflik değişikliğinin hedef kargo yöntemi kimliğini doğruluyorum.
    public SetShippingMethodActivationCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();
    }
}
