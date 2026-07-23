using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    private readonly IProductRepository _productRepository;

    public GetProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    // Burada ürün listesini okuyup dışarıya DTO olarak hazırlıyorum.
    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetListAsync(
            new ProductListFilter(
                request.PageNumber,
                request.PageSize,
                request.Search,
                request.TypeId,
                request.BrandId,
                request.Status,
                request.IsActive,
                request.IsFeatured,
                request.SortBy,
                request.Descending),
            cancellationToken);
        return products.Map(product => product.ToDto());
    }
}
