using ECommerce.Domain.Accounting.CashAndBank;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Persistence.Accounting.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    // Burada ödeme belgesinin cari, finans hesabı, idempotency ve reversal ilişkilerini tanımlıyorum.
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("AccountingPayments", table =>
        {
            table.HasCheckConstraint("CK_AccountingPayments_Amount", "[Amount] > 0 AND [ExchangeRate] > 0");
            table.HasCheckConstraint(
                "CK_AccountingPayments_FinancialAccount",
                "([CashAccountId] IS NOT NULL AND [BankAccountId] IS NULL) OR " +
                "([CashAccountId] IS NULL AND [BankAccountId] IS NOT NULL)");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(item => item.Direction).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(item => item.Amount).HasPrecision(18, 2);
        builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(item => item.ExchangeRate).HasPrecision(18, 6);
        builder.Property(item => item.ReferenceNumber).HasMaxLength(Payment.MaximumReferenceNumberLength);
        builder.Property(item => item.Description).HasMaxLength(Payment.MaximumDescriptionLength);
        builder.Property(item => item.IdempotencyKey).HasMaxLength(Payment.MaximumIdempotencyKeyLength).IsRequired();
        builder.Property(item => item.CancellationReason).HasMaxLength(Payment.MaximumDescriptionLength);
        builder.HasOne(item => item.CurrentAccount)
            .WithMany()
            .HasForeignKey(item => item.CurrentAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.CashAccount)
            .WithMany()
            .HasForeignKey(item => item.CashAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.BankAccount)
            .WithMany()
            .HasForeignKey(item => item.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ReversesPayment)
            .WithMany()
            .HasForeignKey(item => item.ReversesPaymentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.Allocations)
            .WithOne(item => item.Payment)
            .HasForeignKey(item => item.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.IdempotencyKey).IsUnique();
        builder.HasIndex(item => item.ReversesPaymentId)
            .IsUnique()
            .HasFilter("[ReversesPaymentId] IS NOT NULL");
        builder.HasIndex(item => new { item.CurrentAccountId, item.PaymentDate, item.Id });
        builder.Navigation(item => item.Allocations).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    // Burada ödemenin doğrudan cari borç veya alacak hareketine yapılan geçerli tahsisini tanımlıyorum.
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.ToTable("AccountingPaymentAllocations", table =>
        {
            table.HasCheckConstraint("CK_AccountingPaymentAllocations_Amount", "[AllocatedAmount] > 0");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.AllocatedAmount).HasPrecision(18, 2);
        builder.HasOne(item => item.CurrentAccountTransaction)
            .WithMany()
            .HasForeignKey(item => item.CurrentAccountTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.PaymentId, item.CurrentAccountTransactionId }).IsUnique();
        builder.HasIndex(item => new { item.CurrentAccountTransactionId, item.IsReversed });
    }
}

public sealed class CashAccountConfiguration : IEntityTypeConfiguration<CashAccount>
{
    // Burada kasa hesabı ana verisini bakiyesiz ve benzersiz kodla tanımlıyorum.
    public void Configure(EntityTypeBuilder<CashAccount> builder)
    {
        builder.ToTable("AccountingCashAccounts");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Code).HasMaxLength(CashAccount.MaximumCodeLength).IsRequired();
        builder.Property(item => item.Name).HasMaxLength(CashAccount.MaximumNameLength).IsRequired();
        builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.HasIndex(item => item.Code).IsUnique();
    }
}

public sealed class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    // Burada banka hesabı ana verisini bakiyesiz ve benzersiz kodla tanımlıyorum.
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("AccountingBankAccounts");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Code).HasMaxLength(BankAccount.MaximumCodeLength).IsRequired();
        builder.Property(item => item.Name).HasMaxLength(BankAccount.MaximumNameLength).IsRequired();
        builder.Property(item => item.BankName).HasMaxLength(BankAccount.MaximumBankNameLength).IsRequired();
        builder.Property(item => item.Iban).HasMaxLength(BankAccount.MaximumIbanLength);
        builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.HasIndex(item => item.Code).IsUnique();
        builder.HasIndex(item => item.Iban)
            .IsUnique()
            .HasFilter("[Iban] IS NOT NULL");
    }
}

public sealed class FinancialTransactionConfiguration : IEntityTypeConfiguration<FinancialTransaction>
{
    // Burada değişmez kasa ve banka ledger hareketinin kaynak tekilliğini ve hesap sınırını tanımlıyorum.
    public void Configure(EntityTypeBuilder<FinancialTransaction> builder)
    {
        builder.ToTable("AccountingFinancialTransactions", table =>
        {
            table.HasCheckConstraint("CK_AccountingFinancialTransactions_Amount", "[Amount] > 0");
            table.HasCheckConstraint(
                "CK_AccountingFinancialTransactions_Account",
                "([CashAccountId] IS NOT NULL AND [BankAccountId] IS NULL) OR " +
                "([CashAccountId] IS NULL AND [BankAccountId] IS NOT NULL)");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(item => item.Direction).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(item => item.Amount).HasPrecision(18, 2);
        builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(item => item.SourceType).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(FinancialTransaction.MaximumDescriptionLength);
        builder.HasOne(item => item.CashAccount)
            .WithMany()
            .HasForeignKey(item => item.CashAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.BankAccount)
            .WithMany()
            .HasForeignKey(item => item.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ReversesTransaction)
            .WithMany()
            .HasForeignKey(item => item.ReversesTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.SourceType, item.SourceId, item.Type }).IsUnique();
        builder.HasIndex(item => item.ReversesTransactionId)
            .IsUnique()
            .HasFilter("[ReversesTransactionId] IS NOT NULL");
        builder.HasIndex(item => new { item.CashAccountId, item.TransactionDate, item.Id });
        builder.HasIndex(item => new { item.BankAccountId, item.TransactionDate, item.Id });
    }
}
