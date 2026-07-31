using ECommerce.Application.Accounting.Cancellations;
using ECommerce.Domain.Accounting.CashAndBank;
using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.Payments;
using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Accounting.PurchaseInvoices;
using ECommerce.Domain.Accounting.SalesOrders;
using ECommerce.Domain.Accounting.SalesInvoices;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Accounting.Repositories;

public sealed class AccountingCancellationRepository : IAccountingCancellationRepository
{
    private readonly AppDbContext _context;
    public AccountingCancellationRepository(AppDbContext context) => _context = context;

    public Task<AccountingSalesOrder?> GetSalesOrderAsync(Guid id, CancellationToken ct) =>
        _context.Set<AccountingSalesOrder>()
            .Include(x => x.SalesInvoice)
            .Include(x => x.Items).ThenInclude(x => x.StockMovements).ThenInclude(x => x.StockMovement)
            .Include(x => x.Items).ThenInclude(x => x.CostLayerConsumptions).ThenInclude(x => x.InventoryCostLayer)
            .AsSplitQuery().FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<PurchaseInvoice?> GetPurchaseInvoiceAsync(Guid id, CancellationToken ct) =>
        _context.Set<PurchaseInvoice>().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<SalesInvoice?> GetSalesInvoiceAsync(Guid id, CancellationToken ct) =>
        _context.Set<SalesInvoice>().Include(x => x.AccountingSalesOrder).FirstOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<InventoryCostLayer>> GetPurchaseLayersAsync(Guid invoiceId, CancellationToken ct) =>
        await _context.Set<InventoryCostLayer>().Include(x => x.Consumptions)
            .Where(x => x.PurchaseInvoiceLine != null && x.PurchaseInvoiceLine.PurchaseInvoiceId == invoiceId).ToListAsync(ct);
    public Task<ProductVariant?> GetVariantAsync(Guid id, CancellationToken ct) =>
        _context.ProductVariants.Include(x => x.StockMovements).FirstOrDefaultAsync(x => x.Id == id, ct);
    public void AddStockReversal(AccountingSalesOrderStockMovementReversal x) => _context.Add(x);
    public void AddCostReversal(CostLayerConsumptionReversal x) => _context.Add(x);
    public Task<FinancialTransaction?> GetFinancialTransactionAsync(Guid id, CancellationToken ct) =>
        _context.Set<FinancialTransaction>().FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<FinancialTransaction?> GetFinancialEffectForPaymentAsync(Guid paymentId, CancellationToken ct) =>
        _context.Set<FinancialTransaction>().FirstOrDefaultAsync(x =>
            x.SourceType == AccountingSourceType.Payment && x.SourceId == paymentId, ct);
    public Task<bool> HasFinancialReversalAsync(Guid transactionId, CancellationToken ct) =>
        _context.Set<FinancialTransaction>().AnyAsync(x => x.ReversesTransactionId == transactionId, ct);
    public Task<bool> HasValidPaymentAllocationsAsync(AccountingSourceType sourceType, Guid sourceId, CancellationToken ct) =>
        _context.Set<PaymentAllocation>().AnyAsync(x =>
            x.CurrentAccountTransaction.SourceType == sourceType &&
            x.CurrentAccountTransaction.SourceId == sourceId &&
            !x.IsReversed &&
            x.Payment.Status == PaymentStatus.Completed, ct);
}
