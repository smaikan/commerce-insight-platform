using ECommerce.Application.StockMovements.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.StockMovements.Commands.BulkCreateStockMovements;

// Burada toplu istekteki tek bir imzalı stok hareketini taşıyorum.
public sealed record BulkStockMovementItem(
    string ProductVariantSku,
    int QuantityDelta,
    StockMovementType Type,
    string? Reason);

// Burada birden çok stok hareketinin atomik olarak oluşturulması isteğini taşıyorum.
public sealed record BulkCreateStockMovementsCommand(
    IReadOnlyList<BulkStockMovementItem> Movements)
    : IRequest<BulkCreateStockMovementsResultDto>
{
    public const int MaximumBatchSize = 500;
}

// Burada toplu işlemin oluşturduğu hareketleri ve toplam sayısını döndürüyorum.
public sealed record BulkCreateStockMovementsResultDto(
    int MovementCount,
    IReadOnlyList<StockMovementDto> Movements);
