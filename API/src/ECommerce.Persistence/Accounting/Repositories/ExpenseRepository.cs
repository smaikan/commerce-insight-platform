using ECommerce.Application.Accounting.Expenses;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Accounting.Expenses;
using ECommerce.Domain.Accounting.PurchaseInvoices;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Accounting.Repositories;

public sealed class ExpenseRepository : IExpenseRepository
{
    private readonly AppDbContext _context;
    public ExpenseRepository(AppDbContext context) => _context = context;

    public Task AddCategoryAsync(ExpenseCategory x, CancellationToken ct) => _context.AddAsync(x, ct).AsTask();
    public Task<ExpenseCategory?> GetCategoryForUpdateAsync(Guid id, CancellationToken ct) =>
        _context.Set<ExpenseCategory>().FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<bool> CategoryCodeExistsAsync(string code, CancellationToken ct) =>
        _context.Set<ExpenseCategory>().AnyAsync(x => x.Code == code, ct);
    public async Task<PagedResult<ExpenseCategory>> GetCategoriesAsync(int page, int size, CancellationToken ct)
    {
        (page, size) = (Math.Max(1, page), Math.Clamp(size, 1, 100));
        var q = _context.Set<ExpenseCategory>().AsNoTracking().OrderBy(x => x.Code);
        return new(await q.Skip((page - 1) * size).Take(size).ToListAsync(ct), page, size, await q.CountAsync(ct));
    }
    public Task AddExpenseAsync(Expense x, CancellationToken ct) => _context.AddAsync(x, ct).AsTask();
    public async Task<PagedResult<Expense>> GetExpensesAsync(int page, int size, CancellationToken ct)
    {
        (page, size) = (Math.Max(1, page), Math.Clamp(size, 1, 100));
        var q = _context.Set<Expense>().AsNoTracking().OrderByDescending(x => x.ExpenseDate).ThenByDescending(x => x.Id);
        return new(await q.Skip((page - 1) * size).Take(size).ToListAsync(ct), page, size, await q.CountAsync(ct));
    }
    public Task<PurchaseInvoice?> GetInvoiceForExpenseAsync(Guid id, CancellationToken ct) =>
        _context.Set<PurchaseInvoice>().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task AddPurchaseExpenseAsync(PurchaseInvoiceExpense x, CancellationToken ct) => _context.AddAsync(x, ct).AsTask();
    public async Task<IReadOnlyList<PurchaseInvoiceExpense>> GetPurchaseExpensesAsync(Guid invoiceId, bool tracking, CancellationToken ct)
    {
        IQueryable<PurchaseInvoiceExpense> q = _context.Set<PurchaseInvoiceExpense>()
            .Include(x => x.Allocations).ThenInclude(x => x.PurchaseInvoiceLine)
            .Where(x => x.PurchaseInvoiceId == invoiceId);
        if (!tracking) q = q.AsNoTracking();
        return await q.OrderBy(x => x.CreatedAt).ToListAsync(ct);
    }
}
