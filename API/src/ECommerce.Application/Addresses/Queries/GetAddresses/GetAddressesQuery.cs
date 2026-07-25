using ECommerce.Application.Addresses.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Addresses.Queries.GetAddresses;

// Burada oturumdaki kullanıcının isteğe bağlı tür filtresiyle adreslerini listeleme isteğini taşıyorum.
public sealed record GetAddressesQuery(AddressType? Type = null) : IRequest<IReadOnlyList<AddressDto>>;
