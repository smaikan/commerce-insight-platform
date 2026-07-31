using ECommerce.Domain.Accounting.Expenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Accounting.Configurations;

public sealed class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> b)
    {
        b.ToTable("AccountingExpenseCategories");
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> b)
    {
        b.ToTable("AccountingExpenses", t => t.HasCheckConstraint("CK_AccountingExpenses_Amount", "[AmountExcludingVat] > 0"));
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.AmountExcludingVat).HasPrecision(18, 2);
        b.Property(x => x.VatRate).HasPrecision(9, 4);
        b.Property(x => x.VatAmount).HasPrecision(18, 2);
        b.Property(x => x.TotalAmountIncludingVat).HasPrecision(18, 2);
        b.Property(x => x.Description).HasMaxLength(500).IsRequired();
        b.HasOne(x => x.ExpenseCategory).WithMany().HasForeignKey(x => x.ExpenseCategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PurchaseInvoiceExpenseConfiguration : IEntityTypeConfiguration<PurchaseInvoiceExpense>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceExpense> b)
    {
        b.ToTable("AccountingPurchaseInvoiceExpenses", table =>
        {
            table.HasCheckConstraint(
                "CK_AccountingPurchaseInvoiceExpenses_Amount",
                "[AmountExcludingVat] > 0 AND [AmountIncludingVat] >= [AmountExcludingVat]");
            table.HasCheckConstraint(
                "CK_AccountingPurchaseInvoiceExpenses_VatRate",
                "[VatRate] >= 0");
        });
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.AmountExcludingVat).HasPrecision(18, 2);
        b.Property(x => x.VatRate).HasPrecision(9, 4);
        b.Property(x => x.AmountIncludingVat).HasPrecision(18, 2);
        b.Property(x => x.Description).HasMaxLength(500);
        b.HasOne(x => x.PurchaseInvoice).WithMany().HasForeignKey(x => x.PurchaseInvoiceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ExpenseCategory).WithMany().HasForeignKey(x => x.ExpenseCategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Allocations).WithOne(x => x.PurchaseInvoiceExpense)
            .HasForeignKey(x => x.PurchaseInvoiceExpenseId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PurchaseInvoiceExpenseAllocationConfiguration : IEntityTypeConfiguration<PurchaseInvoiceExpenseAllocation>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceExpenseAllocation> b)
    {
        b.ToTable("AccountingPurchaseInvoiceExpenseAllocations", table =>
        {
            table.HasCheckConstraint(
                "CK_AccountingPurchaseInvoiceExpenseAllocations_Amount",
                "[AmountExcludingVat] >= 0 AND [AmountIncludingVat] >= [AmountExcludingVat]");
        });
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.AmountExcludingVat).HasPrecision(18, 2);
        b.Property(x => x.AmountIncludingVat).HasPrecision(18, 2);
        b.HasOne(x => x.PurchaseInvoiceLine).WithMany().HasForeignKey(x => x.PurchaseInvoiceLineId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PurchaseInvoiceExpenseId, x.PurchaseInvoiceLineId }).IsUnique();
    }
}
