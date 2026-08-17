using ECommerce.Application.Addresses.Dtos;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Addresses.Commands.UpdateAddress;

public sealed class UpdateAddressCommandHandler : IRequestHandler<UpdateAddressCommand, AddressDto>
{
    private readonly IAddressRepository _addressRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    // Burada adres güncelleme akışının sahiplik, repository ve transaction bağımlılıklarını hazırlıyorum.
    public UpdateAddressCommandHandler(
        IAddressRepository addressRepository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _addressRepository = addressRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    // Burada yalnız oturumdaki kullanıcıya ait adresi varsayılan seçimle birlikte güvenli biçimde güncelliyorum.
    public Task<AddressDto> Handle(UpdateAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCancellationToken => UpdateInTransactionAsync(
                request,
                userId,
                transactionCancellationToken),
            cancellationToken);
    }

    // Burada adres türü değişse dahi aynı türde tek varsayılan seçimi koruyarak bilgileri kaydediyorum.
    private async Task<AddressDto> UpdateInTransactionAsync(
        UpdateAddressCommand request,
        long userId,
        CancellationToken cancellationToken)
    {
        var address = await _addressRepository.GetByIdForUserForUpdateAsync(
            request.AddressId,
            userId,
            cancellationToken)
            ?? throw new NotFoundException("Address was not found.");
        var previousType = address.Type;

        if (address.IsDefault && (!request.IsDefault || previousType != request.Type))
        {
            address.UnsetDefault();
        }

        if (request.IsDefault)
        {
            var previousDefaults = await _addressRepository.GetDefaultsForUserAndTypeForUpdateAsync(
                userId,
                request.Type,
                address.Id,
                cancellationToken);
            foreach (var previousDefault in previousDefaults)
            {
                previousDefault.UnsetDefault();
            }
        }

        address.Update(
            request.Type,
            request.Title,
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.City,
            request.District,
            request.Neighborhood,
            request.FullAddress,
            request.PostalCode);

        if (request.IsDefault)
        {
            address.SetAsDefault();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return address.ToDto();
    }
}

