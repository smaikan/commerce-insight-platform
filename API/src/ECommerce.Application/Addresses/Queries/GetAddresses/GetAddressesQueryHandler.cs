using ECommerce.Application.Addresses.Dtos;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Addresses.Queries.GetAddresses;

public sealed class GetAddressesQueryHandler : IRequestHandler<GetAddressesQuery, IReadOnlyList<AddressDto>>
{
    private readonly IAddressRepository _addressRepository;
    private readonly ICurrentUserService _currentUser;

    // Burada adres listeleme akışının güvenli sahiplik ve repository bağımlılıklarını hazırlıyorum.
    public GetAddressesQueryHandler(
        IAddressRepository addressRepository,
        ICurrentUserService currentUser)
    {
        _addressRepository = addressRepository;
        _currentUser = currentUser;
    }

    // Burada yalnız oturumdaki kullanıcıya ait adresleri DTO listesine çeviriyorum.
    public async Task<IReadOnlyList<AddressDto>> Handle(
        GetAddressesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var addresses = await _addressRepository.GetByUserIdAsync(
            userId,
            request.Type,
            cancellationToken);

        return addresses.Select(address => address.ToDto()).ToList();
    }
}
