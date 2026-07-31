using ECommerce.Application.Addresses.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Addresses.Commands.UpdateAddress;

// Burada kullanıcının kendi adresindeki iletişim, tür ve varsayılan seçim bilgisini güncelleme isteğini taşıyorum.
public sealed record UpdateAddressCommand(
    Guid AddressId,
    AddressType Type,
    string Title,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string City,
    string District,
    string FullAddress,
    string? PostalCode = null,
    bool IsDefault = false) : IRequest<AddressDto>;
