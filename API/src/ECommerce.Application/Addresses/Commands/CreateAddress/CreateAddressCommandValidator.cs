using ECommerce.Domain.Entities;
using FluentValidation;

namespace ECommerce.Application.Addresses.Commands.CreateAddress;

public sealed class CreateAddressCommandValidator : AbstractValidator<CreateAddressCommand>
{
    // Burada yeni adres isteğinin zorunlu alanlarını ve veritabanı uzunluk sınırlarını doğruluyorum.
    public CreateAddressCommandValidator()
    {
        RuleFor(command => command.Type).IsInEnum();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(Address.MaximumTitleLength);
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(Address.MaximumFirstNameLength);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(Address.MaximumLastNameLength);
        RuleFor(command => command.PhoneNumber).NotEmpty().MaximumLength(Address.MaximumPhoneNumberLength);
        RuleFor(command => command.City).NotEmpty().MaximumLength(Address.MaximumCityLength);
        RuleFor(command => command.District).NotEmpty().MaximumLength(Address.MaximumDistrictLength);
        RuleFor(command => command.Neighborhood).MaximumLength(Address.MaximumNeighborhoodLength);
        RuleFor(command => command.FullAddress).NotEmpty().MaximumLength(Address.MaximumFullAddressLength);
        RuleFor(command => command.PostalCode).MaximumLength(Address.MaximumPostalCodeLength);
    }
}

