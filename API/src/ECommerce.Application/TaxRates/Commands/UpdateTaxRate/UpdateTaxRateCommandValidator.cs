using ECommerce.Domain.Entities;
using FluentValidation;

namespace ECommerce.Application.TaxRates.Commands.UpdateTaxRate;

public sealed class UpdateTaxRateCommandValidator : AbstractValidator<UpdateTaxRateCommand>
{
    // Burada vergi oranı güncellemesinin kimlik, ad ve yüzde sınırlarını doğruluyorum.
    public UpdateTaxRateCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(TaxRate.MaximumNameLength);

        RuleFor(command => command.Rate)
            .InclusiveBetween(TaxRate.MinimumRate, TaxRate.MaximumRate);
    }
}
