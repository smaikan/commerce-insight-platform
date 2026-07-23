using ECommerce.Application.Collections.Dtos;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.Collections.Queries.GetCollections;

public sealed class GetCollectionsQueryHandler : IRequestHandler<GetCollectionsQuery, PagedResult<CollectionDto>>
{
    private readonly ICollectionRepository _collectionRepository;

    public GetCollectionsQueryHandler(ICollectionRepository collectionRepository)
    {
        _collectionRepository = collectionRepository;
    }

    // Burada koleksiyon listesini okuyup DTO olarak hazırlıyorum.
    public async Task<PagedResult<CollectionDto>> Handle(GetCollectionsQuery request, CancellationToken cancellationToken)
    {
        var collections = await _collectionRepository.GetListAsync(request.PageNumber, request.PageSize, cancellationToken);
        return collections.Map(collection => collection.ToDto());
    }
}
