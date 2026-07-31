using FluentValidation;

namespace ECommerce.Application.ShippingMethods.Queries.GetShippingMethods;

public sealed class GetShippingMethodsQueryValidator : AbstractValidator<GetShippingMethodsQuery>
{
    // Burada kargo yöntemi listeleme sorgusunun güvenli sayfalama sınırlarını doğruluyorum.
    public GetShippingMethodsQueryValidator()
    {
        RuleFor(query => query.PageNumber)
            .InclusiveBetween(1, 10_000);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);
    }
}
