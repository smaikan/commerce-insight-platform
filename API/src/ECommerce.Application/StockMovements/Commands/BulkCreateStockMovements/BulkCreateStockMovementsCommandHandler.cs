using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.StockMovements.Dtos;
using MediatR;

namespace ECommerce.Application.StockMovements.Commands.BulkCreateStockMovements;

public sealed class BulkCreateStockMovementsCommandHandler
    : IRequestHandler<BulkCreateStockMovementsCommand, BulkCreateStockMovementsResultDto>
{
    private readonly IProductVariantRepository _variantRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada toplu stok hareketi için varyant deposunu ve transaction birimini hazırlıyorum.
    public BulkCreateStockMovementsCommandHandler(
        IProductVariantRepository variantRepository,
        IUnitOfWork unitOfWork)
    {
        _variantRepository = variantRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada bütün hareketleri tek serializable transaction içinde çalıştırıyorum.
    public Task<BulkCreateStockMovementsResultDto> Handle(
        BulkCreateStockMovementsCommand request,
        CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCancellationToken => CreateMovementsAsync(
                request,
                transactionCancellationToken),
            cancellationToken);
    }

    // Burada tüm varyantları önce doğrulayıp hareketleri istek sırasıyla ve tek kayıt çağrısıyla uyguluyorum.
    private async Task<BulkCreateStockMovementsResultDto> CreateMovementsAsync(
        BulkCreateStockMovementsCommand request,
        CancellationToken cancellationToken)
    {
        var requestedVariantSkus = request.Movements
            .Select(item => item.ProductVariantSku.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(sku => sku, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var variants = await _variantRepository.GetBySkusForUpdateAsync(
            requestedVariantSkus,
            cancellationToken);

        if (variants.Count != requestedVariantSkus.Length)
        {
            throw new NotFoundException("One or more product variants were not found.");
        }

        var variantsBySku = variants.ToDictionary(
            variant => variant.Sku,
            StringComparer.OrdinalIgnoreCase);
        var createdMovements = new List<StockMovementDto>(request.Movements.Count);

        foreach (var item in request.Movements)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var movement = variantsBySku[item.ProductVariantSku.Trim()].ApplyStockMovement(
                item.QuantityDelta,
                item.Type,
                item.Reason);

            createdMovements.Add(movement.ToDto());
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new BulkCreateStockMovementsResultDto(
            createdMovements.Count,
            createdMovements);
    }
}
