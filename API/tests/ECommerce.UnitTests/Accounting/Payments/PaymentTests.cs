using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CashAndBank;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.Payments;
using ECommerce.Domain.Common;
using FluentAssertions;

namespace ECommerce.UnitTests.Accounting.Payments;

public sealed class PaymentTests
{
    // Burada müşteri tahsilatının doğrudan cari alacak hareketine kısmi tahsis edildiğini doğruluyorum.
    [Fact]
    public void CustomerCollection_Should_Allocate_To_Receivable()
    {
        var account = CreateAccount(CurrentAccountType.Customer);
        var receivable = account.AddTransaction(
            CurrentAccountTransactionType.CustomerReceivable,
            100m,
            0m,
            "TRY",
            1m,
            DateTime.UtcNow,
            null,
            AccountingSourceType.AccountingSalesOrder,
            Guid.NewGuid(),
            null);
        var payment = CreatePayment(account, PaymentType.CustomerCollection, 40m);

        var allocation = payment.Allocate(receivable, 40m);

        allocation.CurrentAccountTransactionId.Should().Be(receivable.Id);
        payment.Direction.Should().Be(PaymentDirection.In);
        payment.GetUnallocatedAmount().Should().Be(0m);
    }

    // Burada tek ödemenin tutarından fazla tahsis yapılmasını engelliyorum.
    [Fact]
    public void Payment_Should_Reject_OverAllocation()
    {
        var account = CreateAccount(CurrentAccountType.Customer);
        var first = CreateReceivable(account);
        var second = CreateReceivable(account);
        var payment = CreatePayment(account, PaymentType.CustomerCollection, 50m);
        payment.Allocate(first, 40m);

        var action = () => payment.Allocate(second, 20m);

        action.Should().Throw<DomainException>();
    }

    // Burada iptal edilen ödemenin tahsislerini geçersiz kılıp yeni tahsise kapanmasını doğruluyorum.
    [Fact]
    public void Cancelled_Payment_Should_Invalidate_Allocations()
    {
        var account = CreateAccount(CurrentAccountType.Customer);
        var receivable = CreateReceivable(account);
        var payment = CreatePayment(account, PaymentType.CustomerCollection, 25m);
        payment.Allocate(receivable, 25m);

        payment.MarkCancelled(1, "Hatalı tahsilat");

        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.Allocations.Should().OnlyContain(item => !item.IsValid && item.IsReversed);
        var action = () => payment.Allocate(CreateReceivable(account), 1m);
        action.Should().Throw<DomainException>();
    }

    // Burada onaylı komisyon ve refund tiplerinin finansal çıkış yönünde olduğunu doğruluyorum.
    [Theory]
    [InlineData(FinancialTransactionType.BankCommission)]
    [InlineData(FinancialTransactionType.MarketplaceCommission)]
    [InlineData(FinancialTransactionType.Refund)]
    public void Approved_Commission_And_Refund_Types_Should_Be_Out(
        FinancialTransactionType type)
    {
        var transaction = new FinancialTransaction(
            null,
            Guid.NewGuid(),
            type,
            10m,
            "TRY",
            DateTime.UtcNow,
            AccountingSourceType.FinancialTransaction,
            Guid.NewGuid(),
            1);

        transaction.Direction.Should().Be(FinancialTransactionDirection.Out);
    }

    // Burada test için rolü belirli aktif cari hesap oluşturuyorum.
    private static CurrentAccount CreateAccount(CurrentAccountType type)
    {
        return new CurrentAccount(
            $"ACC-{Guid.NewGuid():N}",
            type,
            "Test Account",
            null, null, null, null, null, null, null, null, null, null, null, null);
    }

    // Burada test için benzersiz kaynaklı müşteri alacağı oluşturuyorum.
    private static CurrentAccountTransaction CreateReceivable(CurrentAccount account)
    {
        return account.AddTransaction(
            CurrentAccountTransactionType.CustomerReceivable,
            100m,
            0m,
            "TRY",
            1m,
            DateTime.UtcNow,
            null,
            AccountingSourceType.AccountingSalesOrder,
            Guid.NewGuid(),
            null);
    }

    // Burada test için kasa bağlantılı tamamlanmış ödeme oluşturuyorum.
    private static Payment CreatePayment(CurrentAccount account, PaymentType type, decimal amount)
    {
        return new Payment(
            account,
            type,
            amount,
            "TRY",
            1m,
            DateTime.UtcNow,
            Guid.NewGuid(),
            null,
            $"IDEMP-{Guid.NewGuid():N}",
            1);
    }
}
