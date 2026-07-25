using FluentValidation;

namespace ECommerce.Application.Addresses.Queries.GetAddresses;

public sealed class GetAddressesQueryValidator : AbstractValidator<GetAddressesQuery>
{
    // Burada varsa tür filtresinin tanımlı bir adres türü olmasını doğruluyorum.
    public GetAddressesQueryValidator()
    {
        RuleFor(query => query.Type)
            .IsInEnum()
            .When(query => query.Type.HasValue);
    }
}
