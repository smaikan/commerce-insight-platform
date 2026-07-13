using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Variants.Queries.GetProductVariantById;

public sealed class GetProductVariantByIdQueryHandler : IRequestHandler<GetProductVariantByIdQuery, ProductVariantDto>
{
    private readonly IProductVariantRepository _variantRepository;

    public GetProductVariantByIdQueryHandler(IProductVariantRepository variantRepository)
    {
        _variantRepository = variantRepository;
    }

    // Burada istenen varyantı okuyup cevap modeline çeviriyorum.
    public async Task<ProductVariantDto> Handle(GetProductVariantByIdQuery request, CancellationToken cancellationToken)
    {
        var variant = await _variantRepository.GetByIdAsync(request.Id, cancellationToken);

        if (variant is null)
        {
            throw new NotFoundException("Product variant was not found.");
        }

        return variant.ToDto();
    }
}
