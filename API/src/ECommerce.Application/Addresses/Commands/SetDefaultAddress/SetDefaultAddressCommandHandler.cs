using ECommerce.Application.Addresses.Dtos;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Addresses.Commands.SetDefaultAddress;

public sealed class SetDefaultAddressCommandHandler : IRequestHandler<SetDefaultAddressCommand, AddressDto>
{
    private readonly IAddressRepository _addressRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    // Burada varsayılan adres seçiminin sahiplik, repository ve transaction bağımlılıklarını hazırlıyorum.
    public SetDefaultAddressCommandHandler(
        IAddressRepository addressRepository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _addressRepository = addressRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    // Burada adresin yalnız sahibinin kendi türündeki varsayılan seçimini serializable transaction içinde değiştiriyorum.
    public Task<AddressDto> Handle(SetDefaultAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCancellationToken => SetDefaultInTransactionAsync(
                request,
                userId,
                transactionCancellationToken),
            cancellationToken);
    }

    // Burada diğer varsayılan adres işaretlerini kaldırıp seçilen adresi atomik olarak varsayılan yapıyorum.
    private async Task<AddressDto> SetDefaultInTransactionAsync(
        SetDefaultAddressCommand request,
        long userId,
        CancellationToken cancellationToken)
    {
        var address = await _addressRepository.GetByIdForUserForUpdateAsync(
            request.AddressId,
            userId,
            cancellationToken)
            ?? throw new NotFoundException("Address was not found.");
        var previousDefaults = await _addressRepository.GetDefaultsForUserAndTypeForUpdateAsync(
            userId,
            address.Type,
            address.Id,
            cancellationToken);
        foreach (var previousDefault in previousDefaults)
        {
            previousDefault.UnsetDefault();
        }

        if (previousDefaults.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        address.SetAsDefault();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return address.ToDto();
    }
}
