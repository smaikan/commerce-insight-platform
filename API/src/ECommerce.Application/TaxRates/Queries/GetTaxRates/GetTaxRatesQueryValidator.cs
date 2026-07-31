using FluentValidation;

namespace ECommerce.Application.TaxRates.Queries.GetTaxRates;

public sealed class GetTaxRatesQueryValidator : AbstractValidator<GetTaxRatesQuery>
{
    // Burada vergi oranı listeleme sorgusunun güvenli sayfalama sınırlarını doğruluyorum.
    public GetTaxRatesQueryValidator()
    {
        RuleFor(query => query.PageNumber)
            .InclusiveBetween(1, 10_000);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);
    }
}
