using ECommerce.Application.Collections.Dtos;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Collections.Queries.GetCollections;

public sealed class GetCollectionsQueryHandler : IRequestHandler<GetCollectionsQuery, IReadOnlyList<CollectionDto>>
{
    private readonly ICollectionRepository _collectionRepository;

    public GetCollectionsQueryHandler(ICollectionRepository collectionRepository)
    {
        _collectionRepository = collectionRepository;
    }

    // Burada koleksiyon listesini okuyup DTO olarak hazırlıyorum.
    public async Task<IReadOnlyList<CollectionDto>> Handle(GetCollectionsQuery request, CancellationToken cancellationToken)
    {
        var collections = await _collectionRepository.GetListAsync(cancellationToken);
        return collections.Select(collection => collection.ToDto()).ToList();
    }
}
