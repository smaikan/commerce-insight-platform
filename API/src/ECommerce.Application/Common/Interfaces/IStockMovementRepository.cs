using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Interfaces;

public interface IStockMovementRepository
{
    // Burada stok hareketlerini filtreli ve sayfalı okuma sözleşmesini tanımlıyorum.
    Task<PagedResult<StockMovement>> GetListAsync(
        StockMovementListFilter filter,
        CancellationToken cancellationToken = default);

    // Burada kayıtlı stok ile hareket toplamını mutabakat için okuma sözleşmesini tanımlıyorum.
    Task<StockBalanceSnapshot?> GetBalanceAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default);
}
