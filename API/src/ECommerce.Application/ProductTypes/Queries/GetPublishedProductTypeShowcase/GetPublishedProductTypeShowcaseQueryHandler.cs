using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.ProductTypes.Dtos;
using MediatR;

namespace ECommerce.Application.ProductTypes.Queries.GetPublishedProductTypeShowcase;

public sealed class GetPublishedProductTypeShowcaseQueryHandler
    : IRequestHandler<GetPublishedProductTypeShowcaseQuery, PagedResult<PublishedProductTypeShowcaseItemDto>>
{
    private readonly IPublishedProductTypeShowcaseReader _reader;
    private readonly IStoreSettingsRepository _storeSettingsRepository;

    // Burada kategori vitrini okuyucusu ile storefront ayar kaynağını hazırlıyorum.
    public GetPublishedProductTypeShowcaseQueryHandler(
        IPublishedProductTypeShowcaseReader reader,
        IStoreSettingsRepository storeSettingsRepository)
    {
        _reader = reader;
        _storeSettingsRepository = storeSettingsRepository;
    }

    // Burada güncel storefront görünürlük tercihlerini public kategori sorgusuna uyguluyorum.
    public async Task<PagedResult<PublishedProductTypeShowcaseItemDto>> Handle(
        GetPublishedProductTypeShowcaseQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await _storeSettingsRepository.GetAsync(asTracking: false, cancellationToken)
            ?? ECommerce.Domain.Entities.StoreSettings.CreateDefault();
        return await _reader.GetListAsync(
            new PublishedProductTypeShowcaseFilter(
                request.PageNumber,
                request.PageSize,
                settings.ShowOutOfStockProducts,
                settings.ShowProductsWithoutPrice),
            cancellationToken);
    }
}
