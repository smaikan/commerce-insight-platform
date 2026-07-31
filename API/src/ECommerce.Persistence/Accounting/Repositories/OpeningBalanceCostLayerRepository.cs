using ECommerce.Application.Accounting.CostLayers;
using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Accounting.Repositories;

public sealed class OpeningBalanceCostLayerRepository
    : IOpeningBalanceCostLayerRepository
{
    private readonly AppDbContext _context;

    // Burada OpeningBalance maliyet katmanı repository'sini aynı request DbContext'iyle hazırlıyorum.
    public OpeningBalanceCostLayerRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni OpeningBalance katmanını ürün ve stok hareketiyle aynı değişiklik takibine ekliyorum.
    public void Add(InventoryCostLayer layer)
    {
        _context.Set<InventoryCostLayer>().Add(layer);
    }

    // Burada yerel takip ve veritabanını birlikte denetleyerek katmanı bulunan OpeningBalance hareketlerini topluca getiriyorum.
    public async Task<IReadOnlySet<Guid>> GetExistingStockMovementIdsAsync(
        IEnumerable<Guid> stockMovementIds,
        CancellationToken cancellationToken = default)
    {
        var movementIds = stockMovementIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        if (movementIds.Length == 0)
        {
            return new HashSet<Guid>();
        }

        var localIds = _context.Set<InventoryCostLayer>().Local
            .Where(layer =>
                layer.SourceType == InventoryCostLayerSourceType.OpeningBalance &&
                movementIds.Contains(layer.StockMovementId))
            .Select(layer => layer.StockMovementId)
            .ToHashSet();
        var databaseIds = await _context.Set<InventoryCostLayer>()
            .Where(layer =>
                layer.SourceType == InventoryCostLayerSourceType.OpeningBalance &&
                movementIds.Contains(layer.StockMovementId))
            .Select(layer => layer.StockMovementId)
            .ToListAsync(cancellationToken);
        localIds.UnionWith(databaseIds);
        return localIds;
    }

    // Burada seçili OpeningBalance katmanını maliyet güncellemesi için takipli getiriyorum.
    public Task<InventoryCostLayer?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _context.Set<InventoryCostLayer>()
            .FirstOrDefaultAsync(
                layer =>
                    layer.Id == id &&
                    layer.SourceType ==
                        InventoryCostLayerSourceType.OpeningBalance,
                cancellationToken);
    }

    // Burada varyantın tek OpeningBalance katmanını salt okunur detay sorgusu için getiriyorum.
    public Task<InventoryCostLayer?> GetByProductVariantIdAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
        return _context.Set<InventoryCostLayer>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                layer =>
                    layer.ProductVariantId == productVariantId &&
                    layer.SourceType ==
                        InventoryCostLayerSourceType.OpeningBalance,
                cancellationToken);
    }
}
