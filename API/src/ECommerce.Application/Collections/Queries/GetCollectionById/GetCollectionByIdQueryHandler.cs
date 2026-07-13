using ECommerce.Application.Collections.Dtos;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Collections.Queries.GetCollectionById;

public sealed class GetCollectionByIdQueryHandler : IRequestHandler<GetCollectionByIdQuery, CollectionDto>
{
    private readonly ICollectionRepository _collectionRepository;

    public GetCollectionByIdQueryHandler(ICollectionRepository collectionRepository)
    {
        _collectionRepository = collectionRepository;
    }

    // Burada istenen koleksiyonu bulup detay cevabına çeviriyorum.
    public async Task<CollectionDto> Handle(GetCollectionByIdQuery request, CancellationToken cancellationToken)
    {
        var collection = await _collectionRepository.GetByIdAsync(request.Id, cancellationToken);

        if (collection is null)
        {
            throw new NotFoundException("Collection was not found.");
        }

        return collection.ToDto();
    }
}
