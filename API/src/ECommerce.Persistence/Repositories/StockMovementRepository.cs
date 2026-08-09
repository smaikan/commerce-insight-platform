using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.StockMovements.Dtos;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class StockMovementRepository : IStockMovementRepository
{
    private readonly AppDbContext _context;

    // Burada stok hareketi sorguları için aynı istek kapsamındaki DbContext'i hazırlıyorum.
    public StockMovementRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada stok defteri satırlarını ürün bağlamıyla filtreli, kararlı sıralı ve sayfalı okuyorum.
    public async Task<PagedResult<StockMovementListItemDto>> GetListAsync(
        StockMovementListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(_context.StockMovements.AsNoTracking(), filter);
        var totalCount = await query.CountAsync(cancellationToken);
        var skip = checked((filter.PageNumber - 1) * filter.PageSize);
        var movements = await query
            .OrderByDescending(movement => movement.CreatedAt)
            .ThenByDescending(movement => movement.Id)
            .Skip(skip)
            .Take(filter.PageSize)
            .Select(movement => new StockMovementListItemDto(
                movement.Id,
                movement.ProductVariantId,
                movement.ProductVariant.Product.Title,
                movement.ProductVariant.Name,
                movement.ProductVariant.Value,
                movement.ProductVariant.Sku,
                movement.Direction,
                movement.Type,
                movement.QuantityDelta,
                movement.StockBeforeMovement,
                movement.StockAfterMovement,
                movement.Reason,
                movement.OrderId,
                movement.ReturnRequestId,
                movement.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<StockMovementListItemDto>(
            movements,
            filter.PageNumber,
            filter.PageSize,
            totalCount);
    }

    // Burada ürün, varyant ve SKU aramasını da içeren stok defteri filtrelerini tek sorguda uyguluyorum.
    private static IQueryable<ECommerce.Domain.Entities.StockMovement> ApplyFilter(
        IQueryable<ECommerce.Domain.Entities.StockMovement> query,
        StockMovementListFilter filter)
    {
        if (filter.ProductVariantId.HasValue)
        {
            query = query.Where(movement => movement.ProductVariantId == filter.ProductVariantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            var normalizedSkuSearch = search.ToUpperInvariant();
            query = query.Where(movement =>
                movement.ProductVariant.Product.Title.Contains(search) ||
                movement.ProductVariant.Name.Contains(search) ||
                movement.ProductVariant.Value.Contains(search) ||
                movement.ProductVariant.Sku.Contains(normalizedSkuSearch));
        }

        if (filter.Direction.HasValue)
        {
            query = query.Where(movement => movement.Direction == filter.Direction.Value);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(movement => movement.Type == filter.Type.Value);
        }

        if (filter.CreatedFromUtc.HasValue)
        {
            query = query.Where(movement => movement.CreatedAt >= filter.CreatedFromUtc.Value);
        }

        if (filter.CreatedToUtc.HasValue)
        {
            query = query.Where(movement => movement.CreatedAt <= filter.CreatedToUtc.Value);
        }

        return query;
    }

    // Burada varyantın kayıtlı stok bakiyesiyle taşma güvenli hareket toplamını mutabakat için okuyorum.
    public async Task<StockBalanceSnapshot?> GetBalanceAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
        var persistedStock = await _context.ProductVariants
            .AsNoTracking()
            .Where(variant => variant.Id == productVariantId)
            .Select(variant => (int?)variant.Stock)
            .SingleOrDefaultAsync(cancellationToken);

        if (!persistedStock.HasValue)
        {
            return null;
        }

        var movementBalance = await _context.StockMovements
            .AsNoTracking()
            .Where(movement => movement.ProductVariantId == productVariantId)
            .SumAsync(movement => (long?)movement.QuantityDelta, cancellationToken)
            ?? 0L;

        return new StockBalanceSnapshot(
            productVariantId,
            persistedStock.Value,
            movementBalance);
    }
}
