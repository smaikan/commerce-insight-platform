using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.StockMovements.Dtos;
using MediatR;

namespace ECommerce.Application.StockMovements.Queries.GetStockBalance;

public sealed class GetStockBalanceQueryHandler : IRequestHandler<GetStockBalanceQuery, StockBalanceDto>
{
    private readonly IStockMovementRepository _stockMovementRepository;

    // Burada stok bakiyesi mutabakatını okuyacak repository bağımlılığını hazırlıyorum.
    public GetStockBalanceQueryHandler(IStockMovementRepository stockMovementRepository)
    {
        _stockMovementRepository = stockMovementRepository;
    }

    // Burada kayıtlı stok ile imzalı hareket toplamını karşılaştırıp tutarlılık sonucunu döndürüyorum.
    public async Task<StockBalanceDto> Handle(
        GetStockBalanceQuery request,
        CancellationToken cancellationToken)
    {
        var snapshot = await _stockMovementRepository.GetBalanceAsync(
            request.ProductVariantId,
            cancellationToken)
            ?? throw new NotFoundException("Product variant was not found.");

        return new StockBalanceDto(
            snapshot.ProductVariantId,
            snapshot.PersistedStock,
            snapshot.MovementBalance,
            snapshot.PersistedStock == snapshot.MovementBalance);
    }
}
