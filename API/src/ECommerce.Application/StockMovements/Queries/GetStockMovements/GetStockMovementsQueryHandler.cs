using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.StockMovements.Dtos;
using MediatR;

namespace ECommerce.Application.StockMovements.Queries.GetStockMovements;

public sealed class GetStockMovementsQueryHandler
    : IRequestHandler<GetStockMovementsQuery, PagedResult<StockMovementDto>>
{
    private readonly IStockMovementRepository _stockMovementRepository;

    // Burada stok hareketi geçmişini okuyacak repository bağımlılığını hazırlıyorum.
    public GetStockMovementsQueryHandler(IStockMovementRepository stockMovementRepository)
    {
        _stockMovementRepository = stockMovementRepository;
    }

    // Burada filtrelenmiş stok hareketlerini kararlı sıralı ve sayfalı DTO listesine dönüştürüyorum.
    public async Task<PagedResult<StockMovementDto>> Handle(
        GetStockMovementsQuery request,
        CancellationToken cancellationToken)
    {
        var movements = await _stockMovementRepository.GetListAsync(
            new StockMovementListFilter(
                request.PageNumber,
                request.PageSize,
                request.ProductVariantId,
                request.Direction,
                request.Type,
                request.CreatedFromUtc,
                request.CreatedToUtc),
            cancellationToken);

        return movements.Map(movement => movement.ToDto());
    }
}
