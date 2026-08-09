using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.StockMovements.Dtos;
using MediatR;

namespace ECommerce.Application.StockMovements.Queries.GetStockMovements;

public sealed class GetStockMovementsQueryHandler
    : IRequestHandler<GetStockMovementsQuery, PagedResult<StockMovementListItemDto>>
{
    private readonly IStockMovementRepository _stockMovementRepository;

    // Burada stok hareketi geçmişini okuyacak repository bağımlılığını hazırlıyorum.
    public GetStockMovementsQueryHandler(IStockMovementRepository stockMovementRepository)
    {
        _stockMovementRepository = stockMovementRepository;
    }

    // Burada filtrelenmiş stok hareketlerini ürün bağlamıyla kararlı sıralı ve sayfalı döndürüyorum.
    public Task<PagedResult<StockMovementListItemDto>> Handle(
        GetStockMovementsQuery request,
        CancellationToken cancellationToken)
    {
        return _stockMovementRepository.GetListAsync(
            new StockMovementListFilter(
                request.PageNumber,
                request.PageSize,
                request.ProductVariantId,
                request.Direction,
                request.Type,
                request.CreatedFromUtc,
                request.CreatedToUtc,
                request.Search),
            cancellationToken);
    }
}
