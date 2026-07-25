using FluentValidation;

namespace ECommerce.Application.ShippingMethods.Queries.GetShippingMethodById;

public sealed class GetShippingMethodByIdQueryValidator : AbstractValidator<GetShippingMethodByIdQuery>
{
    // Burada tekil kargo yöntemi sorgusunun boş olmayan kimlik taşıdığını doğruluyorum.
    public GetShippingMethodByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}
