using MediatR;

namespace ECommerce.Application.Addresses.Commands.DeleteAddress;

// Burada kullanıcının kendi adresini silme isteğini taşıyorum.
public sealed record DeleteAddressCommand(Guid AddressId) : IRequest;
