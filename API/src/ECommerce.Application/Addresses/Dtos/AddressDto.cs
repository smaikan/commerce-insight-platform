using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Addresses.Dtos;

// Burada kullanıcının adres bilgisini iç kimlikleri açmadan istemciye taşıyorum.
public sealed record AddressDto(
    Guid Id,
    AddressType Type,
    string Title,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string City,
    string District,
    string FullAddress,
    string? PostalCode,
    bool IsDefault,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public static class AddressDtoMapping
{
    // Burada adres entity'sini kullanıcıya ait güvenli cevap sözleşmesine çeviriyorum.
    public static AddressDto ToDto(this Address address)
    {
        return new AddressDto(
            address.Id,
            address.Type,
            address.Title,
            address.FirstName,
            address.LastName,
            address.PhoneNumber,
            address.City,
            address.District,
            address.FullAddress,
            address.PostalCode,
            address.IsDefault,
            address.CreatedAt,
            address.UpdatedAt);
    }
}
