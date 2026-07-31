using FluentValidation;

namespace ECommerce.Application.Accounting.Expenses;

public sealed class CreateExpenseCategoryCommandValidator : AbstractValidator<CreateExpenseCategoryCommand>
{
    public CreateExpenseCategoryCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}

public sealed class CreateGeneralExpenseCommandValidator : AbstractValidator<CreateGeneralExpenseCommand>
{
    public CreateGeneralExpenseCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.AmountExcludingVat).GreaterThan(0);
        RuleFor(x => x.VatRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ExpenseDate).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
    }
}

public sealed class AddPurchaseInvoiceExpenseCommandValidator : AbstractValidator<AddPurchaseInvoiceExpenseCommand>
{
    public AddPurchaseInvoiceExpenseCommandValidator()
    {
        RuleFor(x => x.PurchaseInvoiceId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.AllocationMethod).IsInEnum();
        RuleFor(x => x.AmountExcludingVat).GreaterThan(0);
        RuleFor(x => x.VatRate).GreaterThanOrEqualTo(0);
    }
}

public sealed class GetExpenseCategoriesQueryValidator : AbstractValidator<GetExpenseCategoriesQuery>
{
    public GetExpenseCategoriesQueryValidator()
    {
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 10_000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetExpensesQueryValidator : AbstractValidator<GetExpensesQuery>
{
    public GetExpensesQueryValidator()
    {
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 10_000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
