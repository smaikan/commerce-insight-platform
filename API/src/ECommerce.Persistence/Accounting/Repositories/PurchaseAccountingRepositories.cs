using ECommerce.Application.Accounting.PurchaseInvoices;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.PurchaseInvoices;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Accounting.Repositories;

public sealed class CurrentAccountRepository : ICurrentAccountRepository
{
    private readonly AppDbContext _context;

    // Burada cari hesap repository'sini aynı request DbContext'iyle hazırlıyorum.
    public CurrentAccountRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni tedarikçiyi Accounting tablosuna ekliyorum.
    public async Task AddAsync(CurrentAccount account, CancellationToken cancellationToken = default)
    {
        await _context.Set<CurrentAccount>().AddAsync(account, cancellationToken);
    }

    // Burada tedarikçiyi takip etmeden kimliğiyle okuyorum.
    public Task<CurrentAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Set<CurrentAccount>().AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    // Burada tedarikçiyi güncelleme için takipli okuyorum.
    public Task<CurrentAccount?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Set<CurrentAccount>().Include(item => item.Transactions)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    // Burada tedarikçileri kararlı ad ve kimlik sırasıyla sayfalıyorum.
    public async Task<PagedResult<CurrentAccount>> GetListAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Clamp(pageNumber, 1, 10_000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.Set<CurrentAccount>().AsNoTracking();
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(item => item.Name).ThenBy(item => item.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<CurrentAccount>(items, pageNumber, pageSize, count);
    }

    // Burada kanonik tedarikçi kodunun başka kayıtta kullanılıp kullanılmadığını denetliyorum.
    public Task<bool> CodeExistsAsync(
        string code,
        Guid? excludedId = null,
        CancellationToken cancellationToken = default)
    {
        return _context.Set<CurrentAccount>().AnyAsync(
            item => item.Code == code && (!excludedId.HasValue || item.Id != excludedId.Value),
            cancellationToken);
    }

    // Burada yeni cari hareketi kesin olarak Added durumunda izliyorum.
    public void AddTransaction(CurrentAccountTransaction transaction)
    {
        _context.Set<CurrentAccountTransaction>().Add(transaction);
    }
}

public sealed class PurchaseInvoiceRepository : IPurchaseInvoiceRepository
{
    private readonly AppDbContext _context;

    // Burada fatura repository'sini Accounting aggregate grafiğiyle hazırlıyorum.
    public PurchaseInvoiceRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni alış faturasını satırlarıyla birlikte takibe ekliyorum.
    public async Task AddAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default)
    {
        await _context.Set<PurchaseInvoice>().AddAsync(invoice, cancellationToken);
    }

    // Burada alış faturasını detay görüntüleme için takip etmeden getiriyorum.
    public Task<PurchaseInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Graph().AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    // Burada alış faturasını değişiklik ve posting için takipli getiriyorum.
    public Task<PurchaseInvoice?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Graph().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    // Burada alış faturalarını detay grafiğini büyütmeden sayfalı getiriyorum.
    public async Task<PagedResult<PurchaseInvoice>> GetListAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Clamp(pageNumber, 1, 10_000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.Set<PurchaseInvoice>().AsNoTracking();
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.InvoiceDate).ThenByDescending(item => item.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<PurchaseInvoice>(items, pageNumber, pageSize, count);
    }

    // Burada aynı tedarikçi ve fatura numarası birleşiminin tekilliğini denetliyorum.
    public Task<bool> InvoiceNumberExistsAsync(
        Guid currentAccountId,
        string invoiceNumber,
        Guid? excludedId = null,
        CancellationToken cancellationToken = default)
    {
        return _context.Set<PurchaseInvoice>().AnyAsync(
            item => item.CurrentAccountId == currentAccountId &&
                    item.InvoiceNumber == invoiceNumber &&
                    (!excludedId.HasValue || item.Id != excludedId.Value),
            cancellationToken);
    }

    // Burada takipli taslak faturaya eklenen yeni GUID'li satırı kesin olarak Added durumunda izliyorum.
    public void AddLine(PurchaseInvoiceLine line)
    {
        _context.Set<PurchaseInvoiceLine>().Add(line);
    }

    // Burada aggregate'dan çıkarılan taslak satırı EF silme takibine alıyorum.
    public void RemoveLine(PurchaseInvoiceLine line)
    {
        _context.Set<PurchaseInvoiceLine>().Remove(line);
    }

    // Burada mevcut taslak satıra eklenen yeni allocation kaydını açıkça Added durumunda izliyorum.
    public void AddAllocation(PurchaseInvoiceStockAllocation allocation)
    {
        _context.Set<PurchaseInvoiceStockAllocation>().Add(allocation);
    }

    // Burada fatura, tedarikçi, satır ve allocation ilişkilerini tek detay grafiğinde hazırlıyorum.
    private IQueryable<PurchaseInvoice> Graph()
    {
        return _context.Set<PurchaseInvoice>()
            .Include(item => item.CurrentAccount)
            .Include(item => item.Lines)
                .ThenInclude(line => line.Allocations)
            .AsSplitQuery();
    }
}

public sealed class AccountingProductSnapshotReader : IAccountingProductSnapshotReader
{
    private readonly AppDbContext _context;

    // Burada ürün snapshot okuyucusunu yalnız mevcut core tablolarına bağlayarak hazırlıyorum.
    public AccountingProductSnapshotReader(AppDbContext context)
    {
        _context = context;
    }

    // Burada varyant ve bağlı ürünün ticari snapshot alanlarını takip etmeden getiriyorum.
    public Task<ProductVariantSnapshot?> GetByVariantIdAsync(
        Guid variantId,
        CancellationToken cancellationToken = default)
    {
        return _context.ProductVariants.AsNoTracking()
            .Where(variant => variant.Id == variantId)
            .Select(variant => new ProductVariantSnapshot(
                variant.ProductId,
                variant.Id,
                variant.Product.Title,
                variant.Name,
                variant.Sku,
                variant.Barcode,
                variant.Stock))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public sealed class AccountingStockMovementReader : IAccountingStockMovementReader
{
    private readonly AppDbContext _context;

    // Burada allocation sorgularını mevcut StockMovement defterine salt okunur bağlıyorum.
    public AccountingStockMovementReader(AppDbContext context)
    {
        _context = context;
    }

    // Burada yalnız pozitif Purchase hareketlerinin kalan maliyetlendirilebilir miktarını listeliyorum.
    public async Task<IReadOnlyList<AvailableStockMovementDto>> GetEligibleAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
        var query =
            from movement in _context.StockMovements.AsNoTracking()
            where movement.ProductVariantId == productVariantId &&
                  movement.Type == StockMovementType.Purchase &&
                  movement.QuantityDelta > 0
            let allocated = _context.Set<PurchaseInvoiceStockAllocation>()
                .Where(item => item.StockMovementId == movement.Id)
                .Sum(item => (int?)item.AllocatedQuantity) ?? 0
            where allocated < movement.QuantityDelta
            orderby movement.CreatedAt, movement.Id
            select new AvailableStockMovementDto(
                movement.Id,
                movement.ProductVariantId,
                movement.QuantityDelta,
                allocated,
                movement.QuantityDelta - allocated,
                movement.CreatedAt);
        return await query.ToListAsync(cancellationToken);
    }

    // Burada allocation/posting doğrulaması için seçili uygun hareketleri kimlikleriyle getiriyorum.
    public async Task<IReadOnlyDictionary<Guid, StockMovement>> GetEligibleByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var distinctIds = ids.Distinct().ToArray();
        return await _context.StockMovements.AsNoTracking()
            .Where(item => distinctIds.Contains(item.Id) &&
                           item.Type == StockMovementType.Purchase &&
                           item.QuantityDelta > 0)
            .ToDictionaryAsync(item => item.Id, cancellationToken);
    }

    // Burada bir hareketin başka satırlara ayrılmış toplam maliyet miktarını hesaplıyorum.
    public async Task<int> GetAllocatedQuantityAsync(
        Guid stockMovementId,
        Guid? excludedLineId = null,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<PurchaseInvoiceStockAllocation>()
            .Where(item => item.StockMovementId == stockMovementId &&
                           (!excludedLineId.HasValue || item.PurchaseInvoiceLineId != excludedLineId.Value))
            .SumAsync(item => (int?)item.AllocatedQuantity, cancellationToken) ?? 0;
    }
}

public sealed class InventoryCostRepository : IInventoryCostRepository
{
    private readonly AppDbContext _context;

    // Burada maliyet katmanı ve geçmiş repository'sini Accounting tablolarına bağlıyorum.
    public InventoryCostRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada allocation kaynaklı yeni maliyet katmanını takibe ekliyorum.
    public async Task AddLayerAsync(InventoryCostLayer layer, CancellationToken cancellationToken = default)
    {
        await _context.Set<InventoryCostLayer>().AddAsync(layer, cancellationToken);
    }

    // Burada aynı allocation için ikinci maliyet katmanı oluşmasını engelliyorum.
    public Task<bool> LayerExistsForAllocationAsync(
        Guid allocationId,
        CancellationToken cancellationToken = default)
    {
        return _context.Set<InventoryCostLayer>()
            .AnyAsync(item => item.PurchaseInvoiceStockAllocationId == allocationId, cancellationToken);
    }

    // Burada varyantın etkin maliyet geçmişini kapanış için takipli getiriyorum.
    public async Task<ProductVariantCostHistory?> GetActiveHistoryForUpdateAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
        var local = _context.Set<ProductVariantCostHistory>().Local
            .Where(item => item.ProductVariantId == productVariantId && item.ValidTo == null)
            .OrderByDescending(item => item.ValidFrom)
            .ThenByDescending(item => item.Id)
            .FirstOrDefault();
        if (local is not null)
        {
            return local;
        }

        return await _context.Set<ProductVariantCostHistory>()
            .Where(item => item.ProductVariantId == productVariantId && item.ValidTo == null)
            .OrderByDescending(item => item.ValidFrom)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Burada yeni etkin maliyet geçmişi kaydını takibe ekliyorum.
    public async Task AddHistoryAsync(
        ProductVariantCostHistory history,
        CancellationToken cancellationToken = default)
    {
        await _context.Set<ProductVariantCostHistory>().AddAsync(history, cancellationToken);
    }
}
