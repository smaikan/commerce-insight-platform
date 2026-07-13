using ECommerce.Application.Collections.Dtos;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Collections.Commands.SetCollectionActivation;

public sealed class SetCollectionActivationCommandHandler
    : IRequestHandler<SetCollectionActivationCommand, CollectionDto>
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetCollectionActivationCommandHandler(ICollectionRepository collectionRepository, IUnitOfWork unitOfWork)
    {
        _collectionRepository = collectionRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada koleksiyonun aktiflik durumunu değiştiriyorum.
    public async Task<CollectionDto> Handle(SetCollectionActivationCommand request, CancellationToken cancellationToken)
    {
        var collection = await _collectionRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (collection is null)
        {
            throw new NotFoundException("Collection was not found.");
        }

        if (request.IsActive)
        {
            collection.Activate();
        }
        else
        {
            collection.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return collection.ToDto();
    }
}
