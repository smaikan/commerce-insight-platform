using ECommerce.Application.Collections.Dtos;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.Collections.Queries.GetPublishedCollectionShowcase;

public sealed class GetPublishedCollectionShowcaseQueryHandler
    : IRequestHandler<GetPublishedCollectionShowcaseQuery, PagedResult<PublishedCollectionShowcaseItemDto>>
{
    private readonly IPublishedCollectionShowcaseReader _reader;
    private readonly IStoreSettingsRepository _storeSettingsRepository;

    // Burada vitrin okuyucusu ile storefront görünürlük ayarlarının kaynağını hazırlıyorum.
    public GetPublishedCollectionShowcaseQueryHandler(
        IPublishedCollectionShowcaseReader reader,
        IStoreSettingsRepository storeSettingsRepository)
    {
        _reader = reader;
        _storeSettingsRepository = storeSettingsRepository;
    }

    // Burada güncel storefront görünürlük tercihlerini public koleksiyon sorgusuna uyguluyorum.
    public async Task<PagedResult<PublishedCollectionShowcaseItemDto>> Handle(
        GetPublishedCollectionShowcaseQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await _storeSettingsRepository.GetAsync(asTracking: false, cancellationToken)
            ?? ECommerce.Domain.Entities.StoreSettings.CreateDefault();
        return await _reader.GetListAsync(
            new PublishedCollectionShowcaseFilter(
                request.PageNumber,
                request.PageSize,
                settings.ShowOutOfStockProducts,
                settings.ShowProductsWithoutPrice),
            cancellationToken);
    }
}
