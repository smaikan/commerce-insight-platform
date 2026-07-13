using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly IProductRepository _productRepository;

    public GetProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    // Burada ürün listesini okuyup dışarıya DTO olarak hazırlıyorum.
    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetListAsync(cancellationToken);
        return products.Select(product => product.ToDto()).ToList();
    }
}
