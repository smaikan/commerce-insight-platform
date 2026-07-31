using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Products.Variants.Commands.UpdateProductVariant;

public sealed class UpdateProductVariantCommandHandler : IRequestHandler<UpdateProductVariantCommand, ProductVariantDto>
{
    private readonly IProductVariantRepository _variantRepository;
    private readonly IVariantOptionResolver? _variantOptionResolver;
    private readonly IUnitOfWork _unitOfWork;

    // Burada varyant bilgi güncellemesinin repository ve transaction bağımlılıklarını hazırlıyorum.
    public UpdateProductVariantCommandHandler(
        IProductVariantRepository variantRepository,
        IUnitOfWork unitOfWork,
        IVariantOptionResolver? variantOptionResolver = null)
    {
        _variantRepository = variantRepository;
        _variantOptionResolver = variantOptionResolver;
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

        var resolvedOption = _variantOptionResolver is null
            ? null
            : await _variantOptionResolver.ResolveCompositeAsync(request.Name, request.Value, cancellationToken);

        variant.UpdateDetails(
            request.Name,
            request.Value,
            request.Sku,
            request.Barcode,
            request.Material);
        if (resolvedOption is not null)
        {
            variant.ReplaceOptionValues(resolvedOption);
        }

        variant.UpdatePrice(
            request.Price,
            request.CompareAtPrice,
            variant.Product?.TaxRate?.CalculateNetPrice(request.Price) ?? request.Price);

        var previousStock = variant.Stock;
        var stockDifference = request.Stock - previousStock;

        if (stockDifference != 0)
        {
            variant.ApplyStockMovement(
                stockDifference,
                StockMovementType.StockCountAdjustment,
                request.StockAdjustmentReason ?? "Variant stock count updated.");
        }

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
