using ECommerce.Application.Addresses.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Addresses.Commands.CreateAddress;

// Burada oturumdaki kullanıcı için yeni teslimat veya fatura adresi oluşturma isteğini taşıyorum.
public sealed record CreateAddressCommand(
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
