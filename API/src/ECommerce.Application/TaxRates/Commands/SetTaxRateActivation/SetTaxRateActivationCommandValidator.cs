using FluentValidation;

namespace ECommerce.Application.TaxRates.Commands.SetTaxRateActivation;

public sealed class SetTaxRateActivationCommandValidator : AbstractValidator<SetTaxRateActivationCommand>
{
    // Burada aktiflik değişikliğinin hedef vergi oranı kimliğini doğruluyorum.
    public SetTaxRateActivationCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();
    }
}
