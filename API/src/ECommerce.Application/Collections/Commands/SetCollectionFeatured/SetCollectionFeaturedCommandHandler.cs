using ECommerce.Application.Collections.Dtos;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Collections.Commands.SetCollectionFeatured;

public sealed class SetCollectionFeaturedCommandHandler
    : IRequestHandler<SetCollectionFeaturedCommand, CollectionDto>
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetCollectionFeaturedCommandHandler(ICollectionRepository collectionRepository, IUnitOfWork unitOfWork)
    {
        _collectionRepository = collectionRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada koleksiyonun öne çıkarılma durumunu değiştiriyorum.
    public async Task<CollectionDto> Handle(SetCollectionFeaturedCommand request, CancellationToken cancellationToken)
    {
        var collection = await _collectionRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (collection is null)
        {
            throw new NotFoundException("Collection was not found.");
        }

        if (request.IsFeatured)
        {
            collection.MarkAsFeatured();
        }
        else
        {
            collection.UnmarkAsFeatured();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return collection.ToDto();
    }
}
