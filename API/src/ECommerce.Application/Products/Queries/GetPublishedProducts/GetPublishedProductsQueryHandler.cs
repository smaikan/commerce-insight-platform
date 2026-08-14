using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using MediatR;
using ECommerce.Application.Products.Services;

namespace ECommerce.Application.Products.Queries.GetPublishedProducts;

public sealed class GetPublishedProductsQueryHandler
    : IRequestHandler<GetPublishedProductsQuery, PagedResult<PublishedProductListItemDto>>
{
    private readonly IPublishedProductListReader _productListReader;

    // Burada storefront liste sorgusu için salt-okunur veri kaynağını hazırlıyorum.
    public GetPublishedProductsQueryHandler(IPublishedProductListReader productListReader)
    {
        _productListReader = productListReader;
    }

    // Burada yalnız yayımlanmış ürünlerin sıralı listesini döndürüyorum.
    public async Task<PagedResult<PublishedProductListItemDto>> Handle(
        GetPublishedProductsQuery request,
        CancellationToken cancellationToken)
    {
        var normalizedSearch = ProductSearchTextNormalizer.Normalize(request.Search);
        var searchTokens = ProductSearchTextNormalizer.Tokenize(normalizedSearch);
        return await _productListReader.GetListAsync(
            new PublishedProductListFilter(
                request.PageNumber,
                request.PageSize,
                request.TypeId,
                request.BrandId,
                request.CollectionId,
                request.TagId,
                request.SortBy,
                request.Descending,
                true,
                true,
                false,
                5,
                searchTokens.Count == 0 ? null : normalizedSearch,
                searchTokens,
                ProductSearchTextNormalizer.CreateCandidateGrams(searchTokens),
                ResolveStoreSettings: true),
            cancellationToken);
    }
}
