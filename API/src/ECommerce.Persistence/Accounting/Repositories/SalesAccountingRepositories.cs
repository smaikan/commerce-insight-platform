using ECommerce.Application.Accounting.SalesOrders;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Accounting.SalesInvoices;
using ECommerce.Domain.Accounting.SalesOrders;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Accounting.Repositories;

public sealed class AccountingSalesOrderRepository :
    IAccountingSalesOrderRepository
{
    private readonly AppDbContext _context;

    // Burada satış siparişi repository'sini aynı request DbContext'iyle hazırlıyorum.
    public AccountingSalesOrderRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni Accounting satış siparişini bütün aggregate grafiğiyle takibe ekliyorum.
    public async Task AddAsync(
        AccountingSalesOrder order,
        CancellationToken cancellationToken = default)
    {
        await _context.Set<AccountingSalesOrder>().AddAsync(order, cancellationToken);
    }

    // Burada satış siparişini detay görüntüleme için tam grafiğiyle takip etmeden getiriyorum.
    public Task<AccountingSalesOrder?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Graph()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    // Burada satış siparişini draft değişikliği veya posting için tam ve takipli getiriyorum.
    public Task<AccountingSalesOrder?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Graph().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    // Burada tekrarlanan isteğin satış siparişini idempotency anahtarıyla takipli buluyorum.
    public Task<AccountingSalesOrder?> GetByIdempotencyKeyForUpdateAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return Graph().FirstOrDefaultAsync(
            item => item.IdempotencyKey == idempotencyKey,
            cancellationToken);
    }

    // Burada satış siparişlerini fatura bağını koruyarak kararlı biçimde sayfalıyorum.
    public async Task<PagedResult<AccountingSalesOrder>> GetListAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Clamp(pageNumber, 1, 10_000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.Set<AccountingSalesOrder>()
            .AsNoTracking()
            .Include(item => item.SalesInvoice);
        var count = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.OrderDate)
            .ThenByDescending(item => item.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<AccountingSalesOrder>(
            items,
            pageNumber,
            pageSize,
            count);
    }

    // Burada kanonik satış siparişi numarasının başka kayıtta kullanılıp kullanılmadığını denetliyorum.
    public Task<bool> OrderNumberExistsAsync(
        string orderNumber,
        Guid? excludedId = null,
        CancellationToken cancellationToken = default)
    {
        return _context.Set<AccountingSalesOrder>().AnyAsync(
            item => item.OrderNumber == orderNumber &&
                    (!excludedId.HasValue || item.Id != excludedId.Value),
            cancellationToken);
    }

    // Burada mevcut takipli siparişe eklenen yeni item'ı graph keşfine bırakmadan Added durumuna alıyorum.
    public void AddItem(AccountingSalesOrderItem item)
    {
        _context.Entry(item).State = EntityState.Added;
    }

    // Burada aggregate'dan çıkarılan draft satış satırını EF silme takibine alıyorum.
    public void RemoveItem(AccountingSalesOrderItem item)
    {
        _context.Set<AccountingSalesOrderItem>().Remove(item);
    }

    // Burada yeni stok hareketi bağlantısını kesin olarak Added durumunda izliyorum.
    public void AddStockMovementLink(AccountingSalesOrderStockMovement link)
    {
        _context.Set<AccountingSalesOrderStockMovement>().Add(link);
    }

    // Burada sipariş, satır, stok, FIFO ve opsiyonel fatura ilişkilerini tek detay grafiğinde hazırlıyorum.
    private IQueryable<AccountingSalesOrder> Graph()
    {
        return _context.Set<AccountingSalesOrder>()
            .Include(item => item.CurrentAccount)
            .Include(item => item.Items)
                .ThenInclude(line => line.StockMovements)
                .ThenInclude(link => link.StockMovement)
            .Include(item => item.Items)
                .ThenInclude(line => line.CostLayerConsumptions)
                .ThenInclude(consumption => consumption.InventoryCostLayer)
            .Include(item => item.Items)
                .ThenInclude(line => line.CostLayerConsumptions)
                .ThenInclude(consumption => consumption.StockMovement)
            .Include(item => item.SalesInvoice)
                .ThenInclude(invoice => invoice!.Lines)
            .AsSplitQuery();
    }
}

public sealed class SalesInvoiceRepository : ISalesInvoiceRepository
{
    private readonly AppDbContext _context;

    // Burada satış faturası repository'sini aynı request DbContext'iyle hazırlıyorum.
    public SalesInvoiceRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni iç satış faturasını satırları ve sipariş bağıyla takibe ekliyorum.
    public async Task AddAsync(
        SalesInvoice invoice,
        CancellationToken cancellationToken = default)
    {
        await _context.Set<SalesInvoice>().AddAsync(invoice, cancellationToken);
    }

    // Burada satış faturasını detay görüntüleme için takip etmeden getiriyorum.
    public Task<SalesInvoice?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Graph()
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    // Burada satış faturasını posting veya taslak satır değişikliği için bağlı sipariş grafiğiyle takipli getiriyorum.
    public Task<SalesInvoice?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return Graph().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    // Burada satış faturalarını sipariş kimliğini koruyarak kararlı biçimde sayfalıyorum.
    public async Task<PagedResult<SalesInvoice>> GetListAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Clamp(pageNumber, 1, 10_000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.Set<SalesInvoice>().AsNoTracking();
        var count = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.InvoiceDate)
            .ThenByDescending(item => item.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<SalesInvoice>(
            items,
            pageNumber,
            pageSize,
            count);
    }

    // Burada cari hesap ve fatura numarası birleşiminin tekilliğini denetliyorum.
    public Task<bool> InvoiceNumberExistsAsync(
        Guid currentAccountId,
        string invoiceNumber,
        Guid? excludedId = null,
        CancellationToken cancellationToken = default)
    {
        return _context.Set<SalesInvoice>().AnyAsync(
            item => item.CurrentAccountId == currentAccountId &&
                    item.InvoiceNumber == invoiceNumber &&
                    (!excludedId.HasValue || item.Id != excludedId.Value),
            cancellationToken);
    }

    // Burada mevcut takipli faturaya eklenen yeni snapshot satırını kesin olarak Added durumuna alıyorum.
    public void AddLine(SalesInvoiceLine line)
    {
        _context.Entry(line).State = EntityState.Added;
    }

    // Burada fatura snapshot yenilemesinde çıkarılan kalıcı satırı silme takibine alıyorum.
    public void RemoveLine(SalesInvoiceLine line)
    {
        _context.Set<SalesInvoiceLine>().Remove(line);
    }

    // Burada fatura satırları ile bağlı satış siparişi item'larını tek detay grafiğinde hazırlıyorum.
    private IQueryable<SalesInvoice> Graph()
    {
        return _context.Set<SalesInvoice>()
            .Include(item => item.CurrentAccount)
            .Include(item => item.Lines)
                .ThenInclude(line => line.AccountingSalesOrderItem)
                .ThenInclude(orderItem => orderItem.CostLayerConsumptions)
            .Include(item => item.AccountingSalesOrder)
                .ThenInclude(order => order.Items)
            .AsSplitQuery();
    }
}

public sealed class AccountingSalesCatalogReader :
    IAccountingSalesCatalogReader
{
    private readonly AppDbContext _context;

    // Burada satış katalog okuyucusunu yalnız mevcut ürün ve varyant tablolarına bağlıyorum.
    public AccountingSalesCatalogReader(AppDbContext context)
    {
        _context = context;
    }

    // Burada varyantları ürün snapshot, aktiflik ve güncel stok bilgileriyle toplu okuyorum.
    public async Task<IReadOnlyDictionary<Guid, AccountingSalesProductSnapshot>>
        GetByVariantIdsAsync(
            IEnumerable<Guid> productVariantIds,
            CancellationToken cancellationToken = default)
    {
        var ids = productVariantIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        var snapshots = await _context.ProductVariants
            .AsNoTracking()
            .Where(variant => ids.Contains(variant.Id))
            .Select(variant => new AccountingSalesProductSnapshot(
                variant.ProductId,
                variant.Id,
                variant.Product.Title,
                variant.Name,
                variant.Sku,
                variant.Barcode,
                variant.Product.IsActive,
                variant.IsActive,
                variant.Stock))
            .ToListAsync(cancellationToken);
        return snapshots.ToDictionary(item => item.ProductVariantId);
    }
}

public sealed class AccountingSalesCostRepository :
    IAccountingSalesCostRepository
{
    private readonly AppDbContext _context;

    // Burada FIFO maliyet repository'sini aynı transaction DbContext'iyle hazırlıyorum.
    public AccountingSalesCostRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada açık katmanları yerel değişiklikleri de hesaba katarak deterministik FIFO sırasıyla getiriyorum.
    public async Task<IReadOnlyList<InventoryCostLayer>> GetOpenLayersForUpdateAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default)
    {
        var localLayers = _context.Set<InventoryCostLayer>()
            .Local
            .Where(item =>
                item.ProductVariantId == productVariantId &&
                _context.Entry(item).State != EntityState.Deleted)
            .ToArray();
        var databaseLayers = await _context.Set<InventoryCostLayer>()
            .Where(item =>
                item.ProductVariantId == productVariantId &&
                item.Status == CostLayerStatus.Open &&
                item.RemainingQuantity > 0)
            .OrderBy(item => item.CostDate)
            .ThenBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
        var localById = localLayers.ToDictionary(item => item.Id);
        var candidates = databaseLayers
            .Concat(localLayers)
            .GroupBy(item => item.Id)
            .Select(group => localById.TryGetValue(group.Key, out var local)
                ? local
                : group.First());
        return InventoryCostLayer.OrderForFifo(candidates);
    }

    // Burada yeni FIFO tüketimini kesin olarak Added durumunda izliyorum.
    public void AddConsumption(CostLayerConsumption consumption)
    {
        _context.Set<CostLayerConsumption>().Add(consumption);
    }
}
