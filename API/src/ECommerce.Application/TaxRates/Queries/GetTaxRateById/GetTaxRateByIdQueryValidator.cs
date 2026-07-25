using FluentValidation;

namespace ECommerce.Application.TaxRates.Queries.GetTaxRateById;

public sealed class GetTaxRateByIdQueryValidator : AbstractValidator<GetTaxRateByIdQuery>
{
    // Burada tekil vergi oranı sorgusunun boş olmayan kimlik taşıdığını doğruluyorum.
    public GetTaxRateByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}
