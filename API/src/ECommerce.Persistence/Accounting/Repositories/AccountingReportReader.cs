using ECommerce.Application.Accounting.Reports;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Common.Identifiers;
using ECommerce.Domain.Accounting.CashAndBank;
using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.Payments;
using ECommerce.Domain.Accounting.PurchaseInvoices;
using ECommerce.Domain.Accounting.SalesInvoices;
using ECommerce.Domain.Accounting.SalesOrders;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Accounting.Repositories;

public sealed class AccountingReportReader : IAccountingReportReader
{
    private readonly AppDbContext _context;
    public AccountingReportReader(AppDbContext context) => _context = context;

    public async Task<PagedResult<AccountingReportRowDto>> ReadAsync(GetAccountingReportQuery request, CancellationToken ct)
    {
        IEnumerable<AccountingReportRowDto> q = await Build(request).ToListAsync(ct);
        if (request.Id.HasValue) q = q.Where(x => x.Id == request.Id || x.RelatedId == request.Id);
        if (request.From.HasValue) q = q.Where(x => x.Date >= request.From);
        if (request.To.HasValue) q = q.Where(x => x.Date <= request.To);
        if (request.HasSalesInvoice.HasValue) q = q.Where(x => x.HasSalesInvoice == request.HasSalesInvoice);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            q = q.Where(x => (x.Number != null && x.Number.Contains(search)) ||
                             (x.Name != null && x.Name.Contains(search)));
        }
        if (request.Kind is AccountingReportKind.CustomerReceivables or AccountingReportKind.SupplierDebts
            or AccountingReportKind.OverdueReceivables or AccountingReportKind.OverdueDebts)
            q = q.Where(x => x.TertiaryAmount > 0m);
        var count = q.Count();
        var items = q.OrderByDescending(x => x.Date).ThenBy(x => x.Id)
            .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();
        return new(items, request.PageNumber, request.PageSize, count);
    }

    private IQueryable<AccountingReportRowDto> Build(GetAccountingReportQuery r) => r.Kind switch
    {
        AccountingReportKind.Sales => _context.Set<AccountingSalesOrder>().AsNoTracking()
            .Where(x => x.Status == InvoiceStatus.Posted).Select(x => new AccountingReportRowDto(
            x.Id, x.SalesInvoice != null ? x.SalesInvoice.Id : null, x.OrderNumber, x.CurrentAccountNameSnapshot, x.OrderDate, x.DueDate,
            x.GrandTotalIncludingVat, x.TotalCostOfGoodsSold, x.GrossProfitExcludingVat, x.Items.Sum(y => y.StockQuantity),
            x.GrossProfitMargin, x.SalesInvoice != null, x.CurrencyCode)),
        AccountingReportKind.SalesItems => _context.Set<AccountingSalesOrderItem>().AsNoTracking()
            .Where(x => x.AccountingSalesOrder.Status == InvoiceStatus.Posted).Select(x => new AccountingReportRowDto(
            x.Id, x.AccountingSalesOrderId, x.AccountingSalesOrder.OrderNumber, x.ProductNameSnapshot + " / " + x.VariantNameSnapshot,
            x.AccountingSalesOrder.OrderDate, null, x.NetAmountExcludingVat, x.CostOfGoodsSold, x.GrossProfitExcludingVat,
            x.StockQuantity, x.GrossProfitMargin, x.AccountingSalesOrder.SalesInvoice != null, x.AccountingSalesOrder.CurrencyCode)),
        AccountingReportKind.SalesInvoices => _context.Set<SalesInvoice>().AsNoTracking().Select(x => new AccountingReportRowDto(
            x.Id, x.AccountingSalesOrderId, x.InvoiceNumber, x.CurrentAccountNameSnapshot, x.InvoiceDate, x.DueDate,
            x.GrandTotalIncludingVat, x.TotalCostOfGoodsSold, x.GrossProfitExcludingVat, x.Lines.Sum(y => y.StockQuantity),
            x.GrossProfitMargin, true, x.CurrencyCode)),
        AccountingReportKind.PurchaseInvoices => _context.Set<PurchaseInvoice>().AsNoTracking().Select(x => new AccountingReportRowDto(
            x.Id, x.CurrentAccountId, x.InvoiceNumber, x.CurrentAccountNameSnapshot, x.InvoiceDate, x.DueDate,
            x.GrandTotalIncludingVat, x.TotalAllocatedExpenseExcludingVat, x.TotalFinalCostExcludingVat,
            x.Lines.Sum(y => y.StockQuantity), null, null, x.CurrencyCode)),
        AccountingReportKind.UncostedStockMovements => PositiveStockMovements(0, false),
        AccountingReportKind.PartiallyCostedStockMovements => PositiveStockMovements(0, true),
        AccountingReportKind.InventoryCostLayers => CostLayers(false),
        AccountingReportKind.RemainingCostLayers => CostLayers(true),
        AccountingReportKind.CostLayerConsumptions => _context.Set<CostLayerConsumption>().AsNoTracking().Select(x => new AccountingReportRowDto(
            x.Id, x.InventoryCostLayerId, null, null, x.CreatedAt, null, x.TotalCostExcludingVat, x.UnitCostExcludingVat, 0m,
            x.Quantity, null, null, "TRY")),
        AccountingReportKind.ProductVariantCostHistory => _context.Set<ProductVariantCostHistory>().AsNoTracking().Select(x => new AccountingReportRowDto(
            x.Id, x.ProductVariantId, null, null, x.ValidFrom, x.ValidTo, x.NewCostExcludingVat,
            x.PreviousCostExcludingVat ?? 0m, x.NewCostIncludingVat, x.OpeningStockQuantity, null, null, "TRY")),
        AccountingReportKind.WarehouseStockValuation => _context.Set<InventoryCostLayer>().AsNoTracking()
            .Where(x => x.RemainingQuantity > 0).GroupBy(x => x.ProductVariantId).Select(g => new AccountingReportRowDto(
                g.Key, null, null, "Implicit warehouse", g.Max(x => x.CostDate), null,
                g.Sum(x => x.RemainingQuantity * x.UnitCostExcludingVat), 0m, 0m,
                g.Sum(x => x.RemainingQuantity), null, null, "TRY")),
        AccountingReportKind.ProductProfitability => _context.Set<AccountingSalesOrderItem>().AsNoTracking()
            .Where(x => x.AccountingSalesOrder.Status == InvoiceStatus.Posted)
            .GroupBy(x => new { x.ProductId, x.ProductNameSnapshot }).Select(g => new AccountingReportRowDto(
                Guid.Empty, null, PublicIdCodec.EncodeProductId(g.Key.ProductId), g.Key.ProductNameSnapshot, g.Max(x => x.AccountingSalesOrder.OrderDate), null,
                g.Sum(x => x.NetAmountExcludingVat), g.Sum(x => x.CostOfGoodsSold), g.Sum(x => x.GrossProfitExcludingVat),
                g.Sum(x => x.StockQuantity), null, null, "TRY")),
        AccountingReportKind.ProductVariantProfitability => ProfitabilityByVariant(),
        AccountingReportKind.AccountingSalesOrderProfitability => ProfitabilityByOrder(false),
        AccountingReportKind.SalesInvoiceProfitability => ProfitabilityByOrder(true),
        AccountingReportKind.CurrentAccountStatement => CurrentAccounts(false, false, false),
        AccountingReportKind.CustomerReceivables => CurrentAccounts(true, false, false),
        AccountingReportKind.SupplierDebts => CurrentAccounts(false, true, false),
        AccountingReportKind.OverdueReceivables => CurrentAccounts(true, false, true),
        AccountingReportKind.OverdueDebts => CurrentAccounts(false, true, true),
        AccountingReportKind.PaymentsAndCollections => _context.Set<ECommerce.Domain.Accounting.Payments.Payment>().AsNoTracking().Select(x => new AccountingReportRowDto(
            x.Id, x.CurrentAccountId, x.ReferenceNumber, x.CurrentAccount.Name, x.PaymentDate, null,
            x.Amount, x.Allocations.Where(a => !a.IsReversed && a.Payment.Status == PaymentStatus.Completed).Sum(a => a.AllocatedAmount),
            x.Amount - x.Allocations.Where(a => !a.IsReversed && a.Payment.Status == PaymentStatus.Completed).Sum(a => a.AllocatedAmount),
            0, null, null, x.CurrencyCode)),
        AccountingReportKind.CashMovements => Financial(true),
        AccountingReportKind.BankMovements => Financial(false),
        AccountingReportKind.PurchaseVat => _context.Set<PurchaseInvoiceLine>().AsNoTracking()
            .Where(x => x.PurchaseInvoice.Status == InvoiceStatus.Posted)
            .GroupBy(x => x.VatRate).Select(g => new AccountingReportRowDto(Guid.Empty, null, null, null, null, null,
                g.Sum(x => x.NetAmountExcludingVat), g.Sum(x => x.VatAmount), g.Sum(x => x.TotalAmountIncludingVat),
                g.Count(), g.Key, null, "TRY")),
        AccountingReportKind.SalesVat => _context.Set<AccountingSalesOrderItem>().AsNoTracking()
            .Where(x => x.AccountingSalesOrder.Status == InvoiceStatus.Posted)
            .GroupBy(x => x.VatRate).Select(g => new AccountingReportRowDto(Guid.Empty, null, null, null, null, null,
                g.Sum(x => x.NetAmountExcludingVat), g.Sum(x => x.VatAmount), g.Sum(x => x.TotalAmountIncludingVat),
                g.Count(), g.Key, null, "TRY")),
        _ => throw new ArgumentOutOfRangeException(nameof(r.Kind))
    };

    private IQueryable<AccountingReportRowDto> PositiveStockMovements(int _, bool partial)
    {
        var q = _context.StockMovements.AsNoTracking().Where(x => x.QuantityDelta > 0);
        return q.Where(x => partial
            ? _context.Set<InventoryCostLayer>().Where(l => l.StockMovementId == x.Id).Sum(l => (int?)l.OriginalQuantity) > 0 &&
              _context.Set<InventoryCostLayer>().Where(l => l.StockMovementId == x.Id).Sum(l => (int?)l.OriginalQuantity) < x.QuantityDelta
            : (_context.Set<InventoryCostLayer>().Where(l => l.StockMovementId == x.Id).Sum(l => (int?)l.OriginalQuantity) ?? 0) == 0)
            .Select(x => new AccountingReportRowDto(x.Id, x.ProductVariantId, null, null, x.CreatedAt, null, 0m, 0m, 0m,
                x.QuantityDelta, null, null, "TRY"));
    }

    private IQueryable<AccountingReportRowDto> CostLayers(bool remaining) =>
        _context.Set<InventoryCostLayer>().AsNoTracking().Where(x => !remaining || x.RemainingQuantity > 0)
            .Select(x => new AccountingReportRowDto(x.Id, x.ProductVariantId, null, null, x.CostDate, null,
                x.RemainingQuantity * x.UnitCostExcludingVat, x.UnitCostExcludingVat, x.TotalCostExcludingVat,
                remaining ? x.RemainingQuantity : x.OriginalQuantity, null, null, "TRY"));

    private IQueryable<AccountingReportRowDto> ProfitabilityByVariant() =>
        _context.Set<AccountingSalesOrderItem>().AsNoTracking()
            .Where(x => x.AccountingSalesOrder.Status == InvoiceStatus.Posted)
            .GroupBy(x => new { x.ProductVariantId, x.VariantNameSnapshot })
            .Select(g => new AccountingReportRowDto(g.Key.ProductVariantId, null, null, g.Key.VariantNameSnapshot,
                g.Max(x => x.AccountingSalesOrder.OrderDate), null, g.Sum(x => x.NetAmountExcludingVat),
                g.Sum(x => x.CostOfGoodsSold), g.Sum(x => x.GrossProfitExcludingVat), g.Sum(x => x.StockQuantity), null, null, "TRY"));

    private IQueryable<AccountingReportRowDto> ProfitabilityByOrder(bool invoices) =>
        _context.Set<AccountingSalesOrder>().AsNoTracking()
            .Where(x => x.Status == InvoiceStatus.Posted && (!invoices || x.SalesInvoice != null))
            .Select(x => new AccountingReportRowDto(invoices ? x.SalesInvoice!.Id : x.Id, invoices ? x.Id : (x.SalesInvoice != null ? x.SalesInvoice.Id : null),
                invoices ? x.SalesInvoice!.InvoiceNumber : x.OrderNumber, x.CurrentAccountNameSnapshot, x.OrderDate, x.DueDate,
                x.NetAmountExcludingVat, x.TotalCostOfGoodsSold, x.GrossProfitExcludingVat, x.Items.Sum(y => y.StockQuantity),
                x.GrossProfitMargin, x.SalesInvoice != null, x.CurrencyCode));

    private IQueryable<AccountingReportRowDto> CurrentAccounts(bool receivable, bool debt, bool overdue) =>
        _context.Set<CurrentAccountTransaction>().AsNoTracking()
            .Where(x => ((!receivable && !debt) ||
                         (receivable && debt && (x.Type == CurrentAccountTransactionType.CustomerReceivable ||
                                                x.Type == CurrentAccountTransactionType.SupplierDebt)) ||
                         (receivable && !debt && x.Type == CurrentAccountTransactionType.CustomerReceivable) ||
                         (!receivable && debt && x.Type == CurrentAccountTransactionType.SupplierDebt)) &&
                        (!overdue || (x.DueDate != null && x.DueDate < DateTime.UtcNow)))
            .Select(x => new AccountingReportRowDto(x.Id, x.CurrentAccountId, null, x.CurrentAccount.Name, x.TransactionDate, x.DueDate,
                x.DebitAmount, x.CreditAmount,
                (x.DebitAmount > 0 ? x.DebitAmount : x.CreditAmount) -
                _context.Set<PaymentAllocation>().Where(a => a.CurrentAccountTransactionId == x.Id &&
                    !a.IsReversed && a.Payment.Status == PaymentStatus.Completed).Sum(a => a.AllocatedAmount) -
                _context.Set<CurrentAccountTransaction>().Where(reversal =>
                    reversal.SourceType == x.SourceType && reversal.SourceId == x.SourceId &&
                    ((x.Type == CurrentAccountTransactionType.CustomerReceivable &&
                      reversal.Type == CurrentAccountTransactionType.CustomerReceivableReversal) ||
                     (x.Type == CurrentAccountTransactionType.SupplierDebt &&
                      reversal.Type == CurrentAccountTransactionType.SupplierDebtReversal)))
                    .Sum(reversal => reversal.DebitAmount + reversal.CreditAmount),
                0, null, null, x.CurrencyCode));

    private IQueryable<AccountingReportRowDto> Financial(bool cash) =>
        _context.Set<FinancialTransaction>().AsNoTracking().Where(x => cash ? x.CashAccountId != null : x.BankAccountId != null)
            .Select(x => new AccountingReportRowDto(x.Id, cash ? x.CashAccountId : x.BankAccountId, null, null, x.TransactionDate, null,
                x.Direction == FinancialTransactionDirection.In ? x.Amount : -x.Amount, x.Amount, 0m, 0, null, null, x.CurrencyCode));

}
