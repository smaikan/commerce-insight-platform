using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
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

    // Burada stok hareketlerini takip kapalı, filtreli ve kararlı sıralı biçimde sayfalıyorum.
    public async Task<PagedResult<StockMovement>> GetListAsync(
        StockMovementListFilter filter,
        CancellationToken cancellationToken = default)
    {
        IQueryable<StockMovement> query = _context.StockMovements.AsNoTracking();

        if (filter.ProductVariantId.HasValue)
        {
            query = query.Where(movement =>
                movement.ProductVariantId == filter.ProductVariantId.Value);
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

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = checked((filter.PageNumber - 1) * filter.PageSize);
        var movements = await query
            .OrderByDescending(movement => movement.CreatedAt)
            .ThenByDescending(movement => movement.Id)
            .Skip(skip)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<StockMovement>(
            movements,
            filter.PageNumber,
            filter.PageSize,
            totalCount);
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
