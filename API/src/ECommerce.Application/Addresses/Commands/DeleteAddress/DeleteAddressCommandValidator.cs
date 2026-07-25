using FluentValidation;

namespace ECommerce.Application.Addresses.Commands.DeleteAddress;

public sealed class DeleteAddressCommandValidator : AbstractValidator<DeleteAddressCommand>
{
    // Burada silinecek adres kimliğinin boş GUID olmamasını doğruluyorum.
    public DeleteAddressCommandValidator()
    {
        RuleFor(command => command.AddressId).NotEmpty();
    }
}
