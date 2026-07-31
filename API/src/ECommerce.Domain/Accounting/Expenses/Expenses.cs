using ECommerce.Domain.Accounting.PurchaseInvoices;
using ECommerce.Domain.Common;

namespace ECommerce.Domain.Accounting.Expenses;

public enum ExpenseType
{
    General = 1,
    InventoryRelatedPurchase = 2
}

public enum PurchaseExpenseAllocationMethod
{
    VatExclusiveLineAmount = 1,
    Quantity = 2,
    Manual = 3
}

public sealed class ExpenseCategory : AuditableEntity
{
    public const int MaximumCodeLength = 50;
    public const int MaximumNameLength = 150;
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; }

    private ExpenseCategory() { }

    public ExpenseCategory(string code, string name)
    {
        Code = Required(code, MaximumCodeLength).ToUpperInvariant();
        Name = Required(name, MaximumNameLength);
        IsActive = true;
    }

    private static string Required(string value, int length)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > length)
            throw new DomainException("Expense category value is invalid.");
        return value.Trim();
    }
}

public sealed class Expense : AuditableEntity
{
    public Guid ExpenseCategoryId { get; private set; }
    public ExpenseCategory ExpenseCategory { get; private set; } = null!;
    public ExpenseType Type { get; private set; }
    public decimal AmountExcludingVat { get; private set; }
    public decimal VatRate { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal TotalAmountIncludingVat { get; private set; }
    public DateTime ExpenseDate { get; private set; }
    public string Description { get; private set; } = null!;
    public long CreatedBy { get; private set; }

    private Expense() { }

    public Expense(ExpenseCategory category, decimal amountExcludingVat, decimal vatRate,
        DateTime expenseDate, string description, long createdBy)
    {
        if (category is null || !category.IsActive || amountExcludingVat <= 0m ||
            vatRate < 0m || expenseDate == default || string.IsNullOrWhiteSpace(description) || createdBy <= 0)
            throw new DomainException("Valid general expense values are required.");

        ExpenseCategoryId = category.Id;
        ExpenseCategory = category;
        Type = ExpenseType.General;
        AmountExcludingVat = Money(amountExcludingVat);
        VatRate = vatRate;
        VatAmount = Money(AmountExcludingVat * vatRate / 100m);
        TotalAmountIncludingVat = Money(AmountExcludingVat + VatAmount);
        ExpenseDate = expenseDate;
        Description = description.Trim();
        CreatedBy = createdBy;
    }

    private static decimal Money(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}

public sealed class PurchaseInvoiceExpense : AuditableEntity
{
    private readonly List<PurchaseInvoiceExpenseAllocation> _allocations = [];
    public Guid PurchaseInvoiceId { get; private set; }
    public PurchaseInvoice PurchaseInvoice { get; private set; } = null!;
    public Guid ExpenseCategoryId { get; private set; }
    public ExpenseCategory ExpenseCategory { get; private set; } = null!;
    public ExpenseType Type { get; private set; }
    public PurchaseExpenseAllocationMethod AllocationMethod { get; private set; }
    public decimal AmountExcludingVat { get; private set; }
    public decimal VatRate { get; private set; }
    public decimal AmountIncludingVat { get; private set; }
    public string? Description { get; private set; }
    public long CreatedBy { get; private set; }
    public IReadOnlyCollection<PurchaseInvoiceExpenseAllocation> Allocations => _allocations.AsReadOnly();

    private PurchaseInvoiceExpense() { }

    public PurchaseInvoiceExpense(PurchaseInvoice invoice, ExpenseCategory category,
        PurchaseExpenseAllocationMethod method, decimal amountExcludingVat, decimal vatRate,
        string? description, long createdBy)
    {
        invoice.EnsureDraft();
        if (category is null || !category.IsActive || !Enum.IsDefined(method) ||
            amountExcludingVat <= 0m || vatRate < 0m || createdBy <= 0)
            throw new DomainException("Valid inventory-related purchase expense values are required.");

        PurchaseInvoiceId = invoice.Id;
        PurchaseInvoice = invoice;
        ExpenseCategoryId = category.Id;
        ExpenseCategory = category;
        Type = ExpenseType.InventoryRelatedPurchase;
        AllocationMethod = method;
        AmountExcludingVat = Money(amountExcludingVat);
        VatRate = vatRate;
        AmountIncludingVat = Money(AmountExcludingVat * (1m + vatRate / 100m));
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        CreatedBy = createdBy;
    }

    public void AddAllocation(PurchaseInvoiceLine line, decimal excludingVat, decimal includingVat)
    {
        if (line.PurchaseInvoiceId != PurchaseInvoiceId || excludingVat < 0m || includingVat < excludingVat)
            throw new DomainException("Purchase expense allocation is invalid.");
        _allocations.Add(new PurchaseInvoiceExpenseAllocation(this, line, excludingVat, includingVat));
    }

    private static decimal Money(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}

public sealed class PurchaseInvoiceExpenseAllocation : BaseEntity
{
    public Guid PurchaseInvoiceExpenseId { get; private set; }
    public PurchaseInvoiceExpense PurchaseInvoiceExpense { get; private set; } = null!;
    public Guid PurchaseInvoiceLineId { get; private set; }
    public PurchaseInvoiceLine PurchaseInvoiceLine { get; private set; } = null!;
    public decimal AmountExcludingVat { get; private set; }
    public decimal AmountIncludingVat { get; private set; }

    private PurchaseInvoiceExpenseAllocation() { }

    internal PurchaseInvoiceExpenseAllocation(PurchaseInvoiceExpense expense, PurchaseInvoiceLine line,
        decimal excludingVat, decimal includingVat)
    {
        PurchaseInvoiceExpenseId = expense.Id;
        PurchaseInvoiceExpense = expense;
        PurchaseInvoiceLineId = line.Id;
        PurchaseInvoiceLine = line;
        AmountExcludingVat = decimal.Round(excludingVat, 2, MidpointRounding.AwayFromZero);
        AmountIncludingVat = decimal.Round(includingVat, 2, MidpointRounding.AwayFromZero);
    }
}
