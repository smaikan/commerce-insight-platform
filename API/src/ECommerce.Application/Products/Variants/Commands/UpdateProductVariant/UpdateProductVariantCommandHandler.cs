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
            request.Name,
            request.Sku,
            request.Barcode,
            request.Material);

        variant.UpdatePrice(request.Price, request.CompareAtPrice);

        var previousStock = variant.Stock;
        var stockDifference = Math.Abs(request.Stock - previousStock);

        if (stockDifference > 0)
        {
            variant.UpdateStock(request.Stock);
            variant.InventoryTransactions.Add(new InventoryTransaction(
                variant.Id,
                request.Stock > previousStock
                    ? InventoryTransactionType.StockIn
                    : InventoryTransactionType.StockOut,
                stockDifference,
                request.Stock,
                request.StockAdjustmentReason));
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
