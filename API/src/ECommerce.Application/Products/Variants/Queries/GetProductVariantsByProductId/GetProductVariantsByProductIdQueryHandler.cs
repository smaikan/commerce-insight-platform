using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Variants.Queries.GetProductVariantsByProductId;

public sealed class GetProductVariantsByProductIdQueryHandler : IRequestHandler<GetProductVariantsByProductIdQuery, IReadOnlyList<ProductVariantDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductVariantRepository _variantRepository;

    public GetProductVariantsByProductIdQueryHandler(
        IProductRepository productRepository,
        IProductVariantRepository variantRepository)
    {
        _productRepository = productRepository;
        _variantRepository = variantRepository;
    }

    // Burada bir ürüne ait tüm varyantları liste cevabına çeviriyorum.
    public async Task<IReadOnlyList<ProductVariantDto>> Handle(GetProductVariantsByProductIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product was not found.");
        }

        var variants = await _variantRepository.GetByProductIdAsync(request.ProductId, cancellationToken);
        return variants.Select(variant => variant.ToDto()).ToList();
    }
}
