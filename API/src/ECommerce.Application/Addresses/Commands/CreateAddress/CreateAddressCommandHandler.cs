using ECommerce.Application.Addresses.Dtos;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Addresses.Commands.CreateAddress;

public sealed class CreateAddressCommandHandler : IRequestHandler<CreateAddressCommand, AddressDto>
{
    private readonly IAddressRepository _addressRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    // Burada adres oluşturma akışının sahiplik, repository ve transaction bağımlılıklarını hazırlıyorum.
    public CreateAddressCommandHandler(
        IAddressRepository addressRepository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _addressRepository = addressRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    // Burada yeni adresi oturumdaki kullanıcıya bağlayıp varsayılan seçim kuralını transaction içinde koruyorum.
    public Task<AddressDto> Handle(CreateAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCancellationToken => CreateInTransactionAsync(
                request,
                userId,
                transactionCancellationToken),
            cancellationToken);
    }

    // Burada aynı türdeki eski varsayılan adresleri kaldırıp yeni adresi atomik olarak ekliyorum.
    private async Task<AddressDto> CreateInTransactionAsync(
        CreateAddressCommand request,
        long userId,
        CancellationToken cancellationToken)
    {
        if (request.IsDefault)
        {
            var previousDefaults = await _addressRepository.GetDefaultsForUserAndTypeForUpdateAsync(
                userId,
                request.Type,
                cancellationToken: cancellationToken);
            foreach (var previousDefault in previousDefaults)
            {
                previousDefault.UnsetDefault();
            }
        }

        var address = new Address(
            userId,
            request.Type,
            request.Title,
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.City,
            request.District,
            request.FullAddress,
            request.PostalCode,
            request.IsDefault);
        await _addressRepository.AddAsync(address, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return address.ToDto();
    }
}
