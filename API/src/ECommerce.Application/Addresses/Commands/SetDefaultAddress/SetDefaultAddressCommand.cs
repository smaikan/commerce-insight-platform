using ECommerce.Application.Addresses.Dtos;
using MediatR;

namespace ECommerce.Application.Addresses.Commands.SetDefaultAddress;

// Burada kullanıcının bir adresini kendi türü için varsayılan yapma isteğini taşıyorum.
public sealed record SetDefaultAddressCommand(Guid AddressId) : IRequest<AddressDto>;
