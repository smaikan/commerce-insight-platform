using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Products.Variants.Commands.UpdateProductVariantStock;

public sealed class UpdateProductVariantStockCommandHandler : IRequestHandler<UpdateProductVariantStockCommand, ProductVariantDto>
{
    private readonly IProductVariantRepository _variantRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada stok güncelleme bağımlılıklarını hazırlıyorum.
    public UpdateProductVariantStockCommandHandler(IProductVariantRepository variantRepository, IUnitOfWork unitOfWork)
    {
        _variantRepository = variantRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada stok bilgisini Product yerine doğrudan varyant üzerinde güncelliyorum.
    public async Task<ProductVariantDto> Handle(UpdateProductVariantStockCommand request, CancellationToken cancellationToken)
    {
        if (request.Quantity == int.MinValue)
        {
            throw new ConflictException("Quantity is outside the supported range.");
        }

        var variant = await _variantRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (variant is null)
        {
            throw new NotFoundException("Product variant was not found.");
        }

        var transactionType = request.Quantity > 0
            ? InventoryTransactionType.StockIn
            : InventoryTransactionType.StockOut;
        var transactionQuantity = Math.Abs(request.Quantity);

        if (request.Quantity > 0)
        {
            variant.IncreaseStock(transactionQuantity);
        }
        else
        {
            variant.ReduceStock(transactionQuantity);
        }

        variant.InventoryTransactions.Add(new InventoryTransaction(
            variant.Id,
            transactionType,
            transactionQuantity,
            variant.Stock,
            request.Reason));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return variant.ToDto();
    }
}
