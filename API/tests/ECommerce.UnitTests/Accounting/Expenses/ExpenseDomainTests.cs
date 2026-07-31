using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.Expenses;
using ECommerce.Domain.Accounting.PurchaseInvoices;
using FluentAssertions;

namespace ECommerce.UnitTests.Accounting.Expenses;

public sealed class ExpenseDomainTests
{
    [Fact]
    public void GeneralExpense_Should_Not_Change_Purchase_Line_Cost()
    {
        var category = new ExpenseCategory("GEN", "General");
        var invoice = CreateInvoice();
        var line = CreateLine(invoice, 1, 2, 100m);
        var before = line.FinalTotalCostExcludingVat;

        var expense = new Expense(category, 50m, 20m, new DateTime(2026, 7, 27), "Office expense", 1);

        expense.Type.Should().Be(ExpenseType.General);
        expense.TotalAmountIncludingVat.Should().Be(60m);
        line.FinalTotalCostExcludingVat.Should().Be(before);
    }

    [Fact]
    public void PurchaseExpense_Should_Recalculate_Final_Total_And_Unit_Cost()
    {
        var invoice = CreateInvoice();
        var first = CreateLine(invoice, 1, 3, 100m);
        var second = CreateLine(invoice, 2, 2, 100m);

        first.ApplyAllocatedExpense(30.01m, 36.01m);
        second.ApplyAllocatedExpense(19.99m, 23.99m);
        invoice.ApplyExpenseTotals();

        invoice.TotalAllocatedExpenseExcludingVat.Should().Be(50m);
        invoice.TotalFinalCostExcludingVat.Should().Be(550m);
        first.FinalUnitCostExcludingVat.Should().Be(110.0033m);
        second.FinalUnitCostExcludingVat.Should().Be(109.995m);
    }

    [Fact]
    public void InventoryExpense_Should_Require_Draft_Invoice()
    {
        var invoice = CreateInvoice();
        CreateLine(invoice, 1, 1, 100m);
        invoice.MarkPosted(1, DateTime.UtcNow);
        var category = new ExpenseCategory("FREIGHT", "Freight");

        var action = () => new PurchaseInvoiceExpense(invoice, category,
            PurchaseExpenseAllocationMethod.Quantity, 10m, 20m, null, 1);

        action.Should().Throw<ECommerce.Domain.Common.DomainException>();
    }

    private static PurchaseInvoice CreateInvoice() => new(
        new CurrentAccount("SUP-EXP", CurrentAccountType.Supplier, "Supplier", null, null, null,
            null, null, null, null, null, null, null, null, null),
        $"EXP-{Guid.NewGuid():N}", new DateTime(2026, 7, 27), null, "TRY", 1m,
        null, null, null, null, 1);

    private static PurchaseInvoiceLine CreateLine(PurchaseInvoice invoice, int number, int quantity, decimal unit)
    {
        var line = new PurchaseInvoiceLine(invoice, number, 1, Guid.NewGuid(), "Product", "Variant",
            $"SKU-{number}", null, quantity, "ADET", 1m, quantity, PriceEntryMode.ExcludingVat,
            unit, 20m, null, null, null, null, true);
        invoice.AddLine(line, 1);
        var net = quantity * unit;
        line.ApplyCalculation(unit, unit * 1.2m, net, net * 1.2m, 0m, 0m, 0m, 0m, 0m, 0m,
            net, net * .2m, net * 1.2m);
        invoice.ApplyTotals(invoice.Lines.Sum(x => x.GrossAmountExcludingVat),
            invoice.Lines.Sum(x => x.GrossAmountIncludingVat), 0m, 0m, 0m, 0m, 0m, 0m,
            invoice.Lines.Sum(x => x.NetAmountExcludingVat), invoice.Lines.Sum(x => x.VatAmount),
            invoice.Lines.Sum(x => x.TotalAmountIncludingVat));
        return line;
    }
}
