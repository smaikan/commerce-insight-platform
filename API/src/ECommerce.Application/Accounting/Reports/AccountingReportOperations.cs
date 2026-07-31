using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.Accounting.Reports;

public enum AccountingReportKind
{
    Sales, SalesItems, SalesInvoices, PurchaseInvoices,
    UncostedStockMovements, PartiallyCostedStockMovements,
    InventoryCostLayers, RemainingCostLayers, CostLayerConsumptions, ProductVariantCostHistory,
    WarehouseStockValuation, ProductProfitability, ProductVariantProfitability,
    AccountingSalesOrderProfitability, SalesInvoiceProfitability,
    CurrentAccountStatement, CustomerReceivables, SupplierDebts, OverdueReceivables, OverdueDebts,
    PaymentsAndCollections, CashMovements, BankMovements, PurchaseVat, SalesVat
}

public sealed record GetAccountingReportQuery(
    AccountingReportKind Kind,
    int PageNumber = 1,
    int PageSize = 20,
    DateTime? From = null,
    DateTime? To = null,
    Guid? Id = null,
    bool? HasSalesInvoice = null,
    string? Search = null) : IRequest<PagedResult<AccountingReportRowDto>>;

public sealed record AccountingReportRowDto(
    Guid Id,
    Guid? RelatedId,
    string? Number,
    string? Name,
    DateTime? Date,
    DateTime? DueDate,
    decimal Amount,
    decimal SecondaryAmount,
    decimal TertiaryAmount,
    int Quantity,
    decimal? Rate,
    bool? HasSalesInvoice,
    string CurrencyCode);

public interface IAccountingReportReader
{
    Task<PagedResult<AccountingReportRowDto>> ReadAsync(GetAccountingReportQuery query, CancellationToken cancellationToken);
}

public sealed class AccountingReportHandler : IRequestHandler<GetAccountingReportQuery, PagedResult<AccountingReportRowDto>>
{
    private readonly IAccountingReportReader _reader;
    public AccountingReportHandler(IAccountingReportReader reader) => _reader = reader;
    public Task<PagedResult<AccountingReportRowDto>> Handle(GetAccountingReportQuery request, CancellationToken cancellationToken)
        => _reader.ReadAsync(request, cancellationToken);
}
