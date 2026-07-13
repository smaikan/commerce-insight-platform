using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Variants.Commands.UpdateProductVariant;

public sealed class UpdateProductVariantCommandHandler : IRequestHandler<UpdateProductVariantCommand, ProductVariantDto>
{
    private readonly IProductVariantRepository _variantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductVariantCommandHandler(IProductVariantRepository variantRepository, IUnitOfWork unitOfWork)
    {
        _variantRepository = variantRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada varyantın temel bilgilerini güncellemeden önce SKU çakışmasını kontrol ediyorum.
    public async Task<ProductVariantDto> Handle(UpdateProductVariantCommand request, CancellationToken cancellationToken)
    {
        var variant = await _variantRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (variant is null)
        {
            throw new NotFoundException("Product variant was not found.");
        }

        if (await _variantRepository.SkuExistsAsync(request.Sku, request.Id, cancellationToken))
        {
            throw new ConflictException("Product variant SKU already exists.");
        }

        variant.UpdateDetails(
            request.Sku,
            request.Barcode,
            request.Color,
            request.Size,
            request.Material);

        variant.UpdatePrice(request.Price, request.CompareAtPrice);
        variant.UpdateStock(request.Stock);

        if (request.IsActive)
        {
            variant.Activate();
        }
        else
        {
            variant.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return variant.ToDto();
    }
}
