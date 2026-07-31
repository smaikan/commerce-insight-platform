using ECommerce.Domain.Entities;
using FluentValidation;

namespace ECommerce.Application.TaxRates.Commands.CreateTaxRate;

public sealed class CreateTaxRateCommandValidator : AbstractValidator<CreateTaxRateCommand>
{
    // Burada yeni vergi oranı isteğinin ad ve yüzde sınırlarını doğruluyorum.
    public CreateTaxRateCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(TaxRate.MaximumNameLength);

        RuleFor(command => command.Rate)
            .InclusiveBetween(TaxRate.MinimumRate, TaxRate.MaximumRate);
    }
}
