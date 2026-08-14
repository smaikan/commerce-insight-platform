using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Queries.GetPublishedProductFacets;

public sealed class GetPublishedProductFacetsQueryHandler
    : IRequestHandler<GetPublishedProductFacetsQuery, IReadOnlyList<PublishedProductFacetItemDto>>
{
    private readonly IPublishedProductFacetReader _facetReader;
    private readonly IStoreSettingsRepository _storeSettingsRepository;

    // Burada facet sorgusunu çalıştıracak persistence okuyucusunu hazırlıyorum.
    public GetPublishedProductFacetsQueryHandler(
        IPublishedProductFacetReader facetReader,
        IStoreSettingsRepository storeSettingsRepository)
    {
        _facetReader = facetReader;
        _storeSettingsRepository = storeSettingsRepository;
    }

    // Burada HTTP'den bağımsız sorgu alanlarını ortak facet filtresine dönüştürüyorum.
    public async Task<IReadOnlyList<PublishedProductFacetItemDto>> Handle(
        GetPublishedProductFacetsQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await _storeSettingsRepository.GetAsync(asTracking: false, cancellationToken)
            ?? ECommerce.Domain.Entities.StoreSettings.CreateDefault();
        return await _facetReader.GetFacetsAsync(
            request.Dimension,
            new PublishedProductFacetFilter(
                request.TypeId,
                request.BrandId,
                request.CollectionId,
                request.TagId,
                settings.ShowOutOfStockProducts,
                settings.ShowProductsWithoutPrice),
            cancellationToken);
    }
}
