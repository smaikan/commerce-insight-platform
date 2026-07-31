using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Addresses.Commands.DeleteAddress;

public sealed class DeleteAddressCommandHandler : IRequestHandler<DeleteAddressCommand>
{
    private readonly IAddressRepository _addressRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    // Burada güvenli adres silme akışının sahiplik, repository ve iş birimi bağımlılıklarını hazırlıyorum.
    public DeleteAddressCommandHandler(
        IAddressRepository addressRepository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _addressRepository = addressRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    // Burada yalnız sahibine ait ve sipariş geçmişinde kullanılmayan adresi siliyorum.
    public async Task Handle(DeleteAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var address = await _addressRepository.GetByIdForUserForUpdateAsync(
            request.AddressId,
            userId,
            cancellationToken)
            ?? throw new NotFoundException("Address was not found.");

        if (await _addressRepository.IsReferencedByOrderAsync(address.Id, cancellationToken))
        {
            throw new ConflictException("Address cannot be deleted because it is referenced by an order.");
        }

        _addressRepository.Remove(address);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
