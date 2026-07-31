using ECommerce.Application.Accounting.CostLayers;
using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Accounting.Repositories;

public sealed class ProductVariantCostHistoryRepository
    : IProductVariantCostHistoryReadRepository
{
    private readonly AppDbContext _context;

    // Burada salt okunur maliyet geçmişi repository'sini Accounting DbContext'ine bağlıyorum.
    public ProductVariantCostHistoryRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada varyant maliyet geçmişini aynı tarihlerde dahi kararlı olacak kronolojik sırayla getiriyorum.
    public async Task<IReadOnlyList<ProductVariantCostHistory>>
        GetByProductVariantIdAsync(
            Guid productVariantId,
            CancellationToken cancellationToken = default)
    {
        return await _context.Set<ProductVariantCostHistory>()
            .AsNoTracking()
            .Where(history => history.ProductVariantId == productVariantId)
            .OrderBy(history => history.ValidFrom)
            .ThenBy(history => history.CreatedAt)
            .ThenBy(history => history.Id)
            .ToListAsync(cancellationToken);
    }
}
