using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using MediatR;

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
    public Task<PagedResult<PublishedProductListItemDto>> Handle(
        GetPublishedProductsQuery request,
        CancellationToken cancellationToken) =>
        _productListReader.GetListAsync(
            new PublishedProductListFilter(
                request.PageNumber,
                request.PageSize,
                request.TypeId,
                request.BrandId,
                request.CollectionId,
                request.TagId,
                request.SortBy,
                request.Descending),
            cancellationToken);
}
