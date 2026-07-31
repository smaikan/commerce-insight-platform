using ECommerce.API.Security;
using ECommerce.Application.Accounting.Reports;
using ECommerce.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Accounting;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/accounting/reports")]
public sealed class AccountingReportsController : ControllerBase
{
    private readonly ISender _sender;
    public AccountingReportsController(ISender sender) => _sender = sender;

    [HttpGet("sales")] public Task<PagedResult<AccountingReportRowDto>> Sales([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.Sales, f, ct);
    [HttpGet("sales/{id:guid}")] public Task<PagedResult<AccountingReportRowDto>> Sale(Guid id, CancellationToken ct) => Send(AccountingReportKind.Sales, new(Id: id), ct);
    [HttpGet("sales/{id:guid}/items")] public Task<PagedResult<AccountingReportRowDto>> SaleItems(Guid id, [FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.SalesItems, f with { Id = id }, ct);
    [HttpGet("sales-invoices")] public Task<PagedResult<AccountingReportRowDto>> SalesInvoices([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.SalesInvoices, f, ct);
    [HttpGet("sales-invoices/{id:guid}")] public Task<PagedResult<AccountingReportRowDto>> SalesInvoice(Guid id, CancellationToken ct) => Send(AccountingReportKind.SalesInvoices, new(Id: id), ct);
    [HttpGet("purchase-invoices")] public Task<PagedResult<AccountingReportRowDto>> PurchaseInvoices([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.PurchaseInvoices, f, ct);
    [HttpGet("purchase-invoices/{id:guid}")] public Task<PagedResult<AccountingReportRowDto>> PurchaseInvoice(Guid id, CancellationToken ct) => Send(AccountingReportKind.PurchaseInvoices, new(Id: id), ct);
    [HttpGet("stock-movements/uncosted")] public Task<PagedResult<AccountingReportRowDto>> Uncosted([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.UncostedStockMovements, f, ct);
    [HttpGet("stock-movements/partially-costed")] public Task<PagedResult<AccountingReportRowDto>> PartiallyCosted([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.PartiallyCostedStockMovements, f, ct);
    [HttpGet("cost-layers")] public Task<PagedResult<AccountingReportRowDto>> CostLayers([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.InventoryCostLayers, f, ct);
    [HttpGet("cost-layers/remaining")] public Task<PagedResult<AccountingReportRowDto>> RemainingLayers([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.RemainingCostLayers, f, ct);
    [HttpGet("cost-layer-consumptions")] public Task<PagedResult<AccountingReportRowDto>> Consumptions([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.CostLayerConsumptions, f, ct);
    [HttpGet("product-variant-cost-history")] public Task<PagedResult<AccountingReportRowDto>> CostHistory([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.ProductVariantCostHistory, f, ct);
    [HttpGet("warehouse-stock-valuation")] public Task<PagedResult<AccountingReportRowDto>> Valuation([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.WarehouseStockValuation, f, ct);
    [HttpGet("profitability/products")] public Task<PagedResult<AccountingReportRowDto>> ProductProfit([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.ProductProfitability, f, ct);
    [HttpGet("profitability/product-variants")] public Task<PagedResult<AccountingReportRowDto>> VariantProfit([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.ProductVariantProfitability, f, ct);
    [HttpGet("profitability/sales-orders")] public Task<PagedResult<AccountingReportRowDto>> OrderProfit([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.AccountingSalesOrderProfitability, f, ct);
    [HttpGet("profitability/sales-invoices")] public Task<PagedResult<AccountingReportRowDto>> InvoiceProfit([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.SalesInvoiceProfitability, f, ct);
    [HttpGet("current-accounts/{id:guid}/statement")] public Task<PagedResult<AccountingReportRowDto>> Statement(Guid id, [FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.CurrentAccountStatement, f with { Id = id }, ct);
    [HttpGet("receivables")] public Task<PagedResult<AccountingReportRowDto>> Receivables([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.CustomerReceivables, f, ct);
    [HttpGet("debts")] public Task<PagedResult<AccountingReportRowDto>> Debts([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.SupplierDebts, f, ct);
    [HttpGet("overdue-receivables")] public Task<PagedResult<AccountingReportRowDto>> OverdueReceivables([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.OverdueReceivables, f, ct);
    [HttpGet("overdue-debts")] public Task<PagedResult<AccountingReportRowDto>> OverdueDebts([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.OverdueDebts, f, ct);
    [HttpGet("payments-and-collections")] public Task<PagedResult<AccountingReportRowDto>> Payments([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.PaymentsAndCollections, f, ct);
    [HttpGet("cash-movements")] public Task<PagedResult<AccountingReportRowDto>> Cash([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.CashMovements, f, ct);
    [HttpGet("bank-movements")] public Task<PagedResult<AccountingReportRowDto>> Bank([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.BankMovements, f, ct);
    [HttpGet("vat/purchases")] public Task<PagedResult<AccountingReportRowDto>> PurchaseVat([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.PurchaseVat, f, ct);
    [HttpGet("vat/sales")] public Task<PagedResult<AccountingReportRowDto>> SalesVat([FromQuery] ReportFilter f, CancellationToken ct) => Send(AccountingReportKind.SalesVat, f, ct);

    private Task<PagedResult<AccountingReportRowDto>> Send(AccountingReportKind kind, ReportFilter f, CancellationToken ct)
        => _sender.Send(new GetAccountingReportQuery(kind, f.PageNumber, f.PageSize, f.From, f.To,
            f.Id, f.HasSalesInvoice, f.Search), ct);
}

public sealed record ReportFilter(int PageNumber = 1, int PageSize = 20, DateTime? From = null,
    DateTime? To = null, Guid? Id = null, bool? HasSalesInvoice = null, string? Search = null);
