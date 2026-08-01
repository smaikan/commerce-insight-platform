using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Queries.GetPublishedProductByUrl;

public sealed class GetPublishedProductByUrlQueryHandler : IRequestHandler<GetPublishedProductByUrlQuery, ProductSeoDto>
{
    private readonly IProductRepository _productRepository;

    public GetPublishedProductByUrlQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductSeoDto> Handle(
        GetPublishedProductByUrlQuery request,
        CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetPublishedByUrlAsync(request.Url.Trim(), cancellationToken);
        if (product is null)
        {
            throw new NotFoundException("Published product was not found.");
        }

        return product.ToSeoDto();
    }
}
