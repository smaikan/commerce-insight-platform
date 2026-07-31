using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Accounting.Expenses;
using ECommerce.Domain.Accounting.PurchaseInvoices;
using MediatR;

namespace ECommerce.Application.Accounting.Expenses;

public sealed record CreateExpenseCategoryCommand(string Code, string Name) : IRequest<ExpenseCategoryDto>;
public sealed record GetExpenseCategoriesQuery(int PageNumber = 1, int PageSize = 20)
    : IRequest<PagedResult<ExpenseCategoryDto>>;
public sealed record CreateGeneralExpenseCommand(Guid CategoryId, decimal AmountExcludingVat,
    decimal VatRate, DateTime ExpenseDate, string Description) : IRequest<ExpenseDto>;
public sealed record GetExpensesQuery(int PageNumber = 1, int PageSize = 20)
    : IRequest<PagedResult<ExpenseDto>>;
public sealed record ManualExpenseAllocationInput(Guid PurchaseInvoiceLineId, decimal AmountExcludingVat);
public sealed record AddPurchaseInvoiceExpenseCommand(Guid PurchaseInvoiceId, Guid CategoryId,
    PurchaseExpenseAllocationMethod AllocationMethod, decimal AmountExcludingVat, decimal VatRate,
    string? Description, IReadOnlyList<ManualExpenseAllocationInput>? ManualAllocations)
    : IRequest<PurchaseInvoiceExpenseDto>;
public sealed record GetPurchaseInvoiceExpensesQuery(Guid PurchaseInvoiceId)
    : IRequest<IReadOnlyList<PurchaseInvoiceExpenseDto>>;

public sealed record ExpenseCategoryDto(Guid Id, string Code, string Name, bool IsActive);
public sealed record ExpenseDto(Guid Id, Guid CategoryId, ExpenseType Type, decimal AmountExcludingVat,
    decimal VatRate, decimal VatAmount, decimal TotalAmountIncludingVat, DateTime ExpenseDate, string Description);
public sealed record PurchaseInvoiceExpenseAllocationDto(Guid LineId, decimal AmountExcludingVat, decimal AmountIncludingVat);
public sealed record PurchaseInvoiceExpenseDto(Guid Id, Guid PurchaseInvoiceId, Guid CategoryId,
    PurchaseExpenseAllocationMethod AllocationMethod, decimal AmountExcludingVat, decimal AmountIncludingVat,
    IReadOnlyList<PurchaseInvoiceExpenseAllocationDto> Allocations);

public interface IExpenseRepository
{
    Task AddCategoryAsync(ExpenseCategory category, CancellationToken cancellationToken);
    Task<ExpenseCategory?> GetCategoryForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> CategoryCodeExistsAsync(string code, CancellationToken cancellationToken);
    Task<PagedResult<ExpenseCategory>> GetCategoriesAsync(int page, int size, CancellationToken cancellationToken);
    Task AddExpenseAsync(Expense expense, CancellationToken cancellationToken);
    Task<PagedResult<Expense>> GetExpensesAsync(int page, int size, CancellationToken cancellationToken);
    Task<PurchaseInvoice?> GetInvoiceForExpenseAsync(Guid id, CancellationToken cancellationToken);
    Task AddPurchaseExpenseAsync(PurchaseInvoiceExpense expense, CancellationToken cancellationToken);
    Task<IReadOnlyList<PurchaseInvoiceExpense>> GetPurchaseExpensesAsync(Guid invoiceId, bool tracking, CancellationToken cancellationToken);
}

public sealed class ExpenseHandlers :
    IRequestHandler<CreateExpenseCategoryCommand, ExpenseCategoryDto>,
    IRequestHandler<GetExpenseCategoriesQuery, PagedResult<ExpenseCategoryDto>>,
    IRequestHandler<CreateGeneralExpenseCommand, ExpenseDto>,
    IRequestHandler<GetExpensesQuery, PagedResult<ExpenseDto>>,
    IRequestHandler<AddPurchaseInvoiceExpenseCommand, PurchaseInvoiceExpenseDto>,
    IRequestHandler<GetPurchaseInvoiceExpensesQuery, IReadOnlyList<PurchaseInvoiceExpenseDto>>
{
    private readonly IExpenseRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public ExpenseHandlers(IExpenseRepository repository, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<ExpenseCategoryDto> Handle(CreateExpenseCategoryCommand request, CancellationToken ct)
    {
        if (await _repository.CategoryCodeExistsAsync(request.Code.Trim().ToUpperInvariant(), ct))
            throw new ConflictException("Expense category code already exists.");
        var category = new ExpenseCategory(request.Code, request.Name);
        await _repository.AddCategoryAsync(category, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(category);
    }

    public async Task<PagedResult<ExpenseCategoryDto>> Handle(GetExpenseCategoriesQuery request, CancellationToken ct)
        => (await _repository.GetCategoriesAsync(request.PageNumber, request.PageSize, ct)).Map(Map);

    public async Task<ExpenseDto> Handle(CreateGeneralExpenseCommand request, CancellationToken ct)
    {
        var category = await _repository.GetCategoryForUpdateAsync(request.CategoryId, ct)
            ?? throw new NotFoundException("Expense category was not found.");
        var expense = new Expense(category, request.AmountExcludingVat, request.VatRate,
            request.ExpenseDate, request.Description, _currentUser.GetRequiredUserId());
        await _repository.AddExpenseAsync(expense, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(expense);
    }

    public async Task<PagedResult<ExpenseDto>> Handle(GetExpensesQuery request, CancellationToken ct)
        => (await _repository.GetExpensesAsync(request.PageNumber, request.PageSize, ct)).Map(Map);

    public async Task<PurchaseInvoiceExpenseDto> Handle(AddPurchaseInvoiceExpenseCommand request, CancellationToken ct)
    {
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async token =>
        {
            var invoice = await _repository.GetInvoiceForExpenseAsync(request.PurchaseInvoiceId, token)
                ?? throw new NotFoundException("Purchase invoice was not found.");
            invoice.EnsureDraft();
            var category = await _repository.GetCategoryForUpdateAsync(request.CategoryId, token)
                ?? throw new NotFoundException("Expense category was not found.");
            var expense = new PurchaseInvoiceExpense(invoice, category, request.AllocationMethod,
                request.AmountExcludingVat, request.VatRate, request.Description, _currentUser.GetRequiredUserId());

            var existing = await _repository.GetPurchaseExpensesAsync(invoice.Id, true, token);
            var previousByLine = existing.SelectMany(x => x.Allocations)
                .GroupBy(x => x.PurchaseInvoiceLineId)
                .ToDictionary(x => x.Key, x => (Ex: x.Sum(y => y.AmountExcludingVat), Inc: x.Sum(y => y.AmountIncludingVat)));
            var shares = CalculateShares(invoice, request);
            var includingTotal = expense.AmountIncludingVat;
            decimal usedEx = 0m, usedInc = 0m;
            for (var i = 0; i < invoice.Lines.Count; i++)
            {
                var line = invoice.Lines.OrderBy(x => x.LineNumber).ElementAt(i);
                var ex = i == invoice.Lines.Count - 1
                    ? expense.AmountExcludingVat - usedEx
                    : decimal.Round(expense.AmountExcludingVat * shares[line.Id], 2, MidpointRounding.AwayFromZero);
                var inc = i == invoice.Lines.Count - 1
                    ? includingTotal - usedInc
                    : decimal.Round(expense.AmountIncludingVat * shares[line.Id], 2, MidpointRounding.AwayFromZero);
                expense.AddAllocation(line, ex, inc);
                previousByLine.TryGetValue(line.Id, out var previous);
                line.ApplyAllocatedExpense(previous.Ex + ex, previous.Inc + inc);
                usedEx += ex;
                usedInc += inc;
            }

            invoice.ApplyExpenseTotals();
            await _repository.AddPurchaseExpenseAsync(expense, token);
            await _unitOfWork.SaveChangesAsync(token);
            return Map(expense);
        }, ct);
    }

    public async Task<IReadOnlyList<PurchaseInvoiceExpenseDto>> Handle(GetPurchaseInvoiceExpensesQuery request, CancellationToken ct)
        => (await _repository.GetPurchaseExpensesAsync(request.PurchaseInvoiceId, false, ct)).Select(Map).ToArray();

    private static Dictionary<Guid, decimal> CalculateShares(PurchaseInvoice invoice, AddPurchaseInvoiceExpenseCommand request)
    {
        var lines = invoice.Lines.OrderBy(x => x.LineNumber).ToArray();
        if (request.AllocationMethod == PurchaseExpenseAllocationMethod.Manual)
        {
            var manual = request.ManualAllocations ?? [];
            if (manual.Count != lines.Length || manual.Select(x => x.PurchaseInvoiceLineId).Distinct().Count() != lines.Length ||
                manual.Any(x => x.AmountExcludingVat < 0m) ||
                !manual.Select(x => x.PurchaseInvoiceLineId).ToHashSet().SetEquals(lines.Select(x => x.Id)) ||
                decimal.Round(manual.Sum(x => x.AmountExcludingVat), 2, MidpointRounding.AwayFromZero) !=
                decimal.Round(request.AmountExcludingVat, 2, MidpointRounding.AwayFromZero))
                throw new ConflictException("Manual allocations must cover every line and equal the expense amount.");
            return manual.ToDictionary(x => x.PurchaseInvoiceLineId,
                x => x.AmountExcludingVat / request.AmountExcludingVat);
        }

        var weights = lines.ToDictionary(x => x.Id, x => request.AllocationMethod == PurchaseExpenseAllocationMethod.Quantity
            ? x.StockQuantity
            : x.NetAmountExcludingVat);
        var total = weights.Values.Sum();
        if (total <= 0m) throw new ConflictException("Purchase invoice lines do not contain an allocatable base.");
        return weights.ToDictionary(x => x.Key, x => x.Value / total);
    }

    private static ExpenseCategoryDto Map(ExpenseCategory x) => new(x.Id, x.Code, x.Name, x.IsActive);
    private static ExpenseDto Map(Expense x) => new(x.Id, x.ExpenseCategoryId, x.Type, x.AmountExcludingVat,
        x.VatRate, x.VatAmount, x.TotalAmountIncludingVat, x.ExpenseDate, x.Description);
    private static PurchaseInvoiceExpenseDto Map(PurchaseInvoiceExpense x) => new(x.Id, x.PurchaseInvoiceId,
        x.ExpenseCategoryId, x.AllocationMethod, x.AmountExcludingVat, x.AmountIncludingVat,
        x.Allocations.OrderBy(y => y.PurchaseInvoiceLine.LineNumber)
            .Select(y => new PurchaseInvoiceExpenseAllocationDto(y.PurchaseInvoiceLineId, y.AmountExcludingVat, y.AmountIncludingVat)).ToArray());
}
