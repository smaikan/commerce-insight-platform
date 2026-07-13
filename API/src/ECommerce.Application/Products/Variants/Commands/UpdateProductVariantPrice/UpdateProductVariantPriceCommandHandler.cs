using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Variants.Commands.UpdateProductVariantPrice;

public sealed class UpdateProductVariantPriceCommandHandler : IRequestHandler<UpdateProductVariantPriceCommand, ProductVariantDto>
{
    private readonly IProductVariantRepository _variantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductVariantPriceCommandHandler(IProductVariantRepository variantRepository, IUnitOfWork unitOfWork)
    {
        _variantRepository = variantRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada varyant fiyatını domain kuralından geçirerek güncelliyorum.
    public async Task<ProductVariantDto> Handle(UpdateProductVariantPriceCommand request, CancellationToken cancellationToken)
    {
        var variant = await _variantRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (variant is null)
        {
            throw new NotFoundException("Product variant was not found.");
        }

        variant.UpdatePrice(request.Price, request.CompareAtPrice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return variant.ToDto();
    }
}
