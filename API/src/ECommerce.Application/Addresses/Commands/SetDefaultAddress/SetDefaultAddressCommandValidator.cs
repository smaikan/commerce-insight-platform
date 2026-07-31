using FluentValidation;

namespace ECommerce.Application.Addresses.Commands.SetDefaultAddress;

public sealed class SetDefaultAddressCommandValidator : AbstractValidator<SetDefaultAddressCommand>
{
    // Burada varsayılan yapılacak adres kimliğinin boş GUID olmamasını doğruluyorum.
    public SetDefaultAddressCommandValidator()
    {
        RuleFor(command => command.AddressId).NotEmpty();
    }
}
