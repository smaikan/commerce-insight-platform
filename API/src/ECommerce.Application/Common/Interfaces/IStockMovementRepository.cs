using ECommerce.Application.Common.Models;
using ECommerce.Application.StockMovements.Dtos;

namespace ECommerce.Application.Common.Interfaces;

public interface IStockMovementRepository
{
    // Burada stok hareketlerini filtreli ve sayfalı okuma sözleşmesini tanımlıyorum.
    Task<PagedResult<StockMovementListItemDto>> GetListAsync(
        StockMovementListFilter filter,
        CancellationToken cancellationToken = default);

    // Burada kayıtlı stok ile hareket toplamını mutabakat için okuma sözleşmesini tanımlıyorum.
    Task<StockBalanceSnapshot?> GetBalanceAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default);
}
