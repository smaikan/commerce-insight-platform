using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Queries.GetProductSeoIndex;

public sealed class GetProductSeoIndexQueryHandler
    : IRequestHandler<GetProductSeoIndexQuery, PagedResult<ProductSeoIndexItemDto>>
{
    private readonly IProductRepository _productRepository;

    public GetProductSeoIndexQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public Task<PagedResult<ProductSeoIndexItemDto>> Handle(
        GetProductSeoIndexQuery request,
        CancellationToken cancellationToken) =>
        _productRepository.GetPublishedSeoIndexAsync(
            request.PageNumber,
            request.PageSize,
            cancellationToken);
}
