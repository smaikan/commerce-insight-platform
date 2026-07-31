using ECommerce.Application.Common.Models;
using ECommerce.Domain.Accounting.CashAndBank;
using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.Payments;

namespace ECommerce.Application.Accounting.Payments;

public sealed record PaymentAllocationInput(Guid CurrentAccountTransactionId, decimal Amount);

public sealed record CreatePaymentInput(
    Guid CurrentAccountId,
    PaymentType Type,
    decimal Amount,
    DateTime PaymentDate,
    IReadOnlyList<PaymentAllocationInput> Allocations,
    Guid? CashAccountId = null,
    Guid? BankAccountId = null,
    string CurrencyCode = "TRY",
    decimal ExchangeRate = 1m,
    string? ReferenceNumber = null,
    string? Description = null);

public sealed record PaymentAllocationDto(
    Guid Id,
    Guid CurrentAccountTransactionId,
    AccountingSourceType SourceType,
    Guid SourceId,
    decimal AllocatedAmount,
    bool IsReversed,
    DateTime? ReversedAt);

public sealed record PaymentDto(
    Guid Id,
    Guid CurrentAccountId,
    PaymentType Type,
    PaymentDirection Direction,
    PaymentStatus Status,
    decimal Amount,
    decimal AllocatedAmount,
    decimal UnallocatedAmount,
    string CurrencyCode,
    DateTime PaymentDate,
    Guid? CashAccountId,
    Guid? BankAccountId,
    string? ReferenceNumber,
    string? Description,
    DateTime CreatedAt,
    long? CancelledBy,
    DateTime? CancelledAt,
    string? CancellationReason,
    IReadOnlyList<PaymentAllocationDto> Allocations);

public sealed record PaymentSummaryDto(
    Guid Id,
    Guid CurrentAccountId,
    PaymentType Type,
    PaymentDirection Direction,
    PaymentStatus Status,
    decimal Amount,
    string CurrencyCode,
    DateTime PaymentDate);

public interface IPaymentRepository
{
    // Burada yeni muhasebe ödemesini kalıcı depoya ekleme sözleşmesini tanımlıyorum.
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
    // Burada ödeme detayını tahsis hedefleriyle birlikte takip etmeden okuyorum.
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Payment?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada aynı istemci tekrarını ikinci finansal etki oluşturmadan buluyorum.
    Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    // Burada ödemeleri kararlı ve sayfalı olarak getiriyorum.
    Task<PagedResult<Payment>> GetListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    // Burada tahsis hedefi cari hareketleri eşzamanlı güncelleme kontrolü için takipli getiriyorum.
    Task<IReadOnlyDictionary<Guid, CurrentAccountTransaction>> GetTransactionsForAllocationAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default);
    // Burada cari hareketin geçerli ödeme tahsis toplamını ledger kayıtlarından hesaplıyorum.
    Task<decimal> GetValidAllocatedAmountAsync(Guid currentAccountTransactionId, CancellationToken cancellationToken = default);
    // Burada kaynak belgesi terslenmiş veya iptal edilmiş cari hareketleri yeni tahsislere kapatıyorum.
    Task<bool> IsTransactionReversedAsync(CurrentAccountTransaction transaction, CancellationToken cancellationToken = default);
    // Burada kaynak belgenin gösterim bakiyesini aynı cari hareketin geçerli tahsis toplamıyla eşitliyorum.
    Task SynchronizeSourcePaymentBalanceAsync(
        CurrentAccountTransaction transaction,
        decimal paidAmount,
        CancellationToken cancellationToken = default);
    // Burada yeni ödeme tahsisini kesin Added durumunda izliyorum.
    void AddAllocation(PaymentAllocation allocation);
}

public sealed record FinancialAccountInput(string Code, string Name, string CurrencyCode = "TRY");
public sealed record BankAccountInput(
    string Code,
    string Name,
    string BankName,
    string? Iban = null,
    string CurrencyCode = "TRY");

public sealed record CashAccountDto(
    Guid Id,
    string Code,
    string Name,
    string CurrencyCode,
    bool IsActive,
    decimal Balance);

public sealed record BankAccountDto(
    Guid Id,
    string Code,
    string Name,
    string BankName,
    string? Iban,
    string CurrencyCode,
    bool IsActive,
    decimal Balance);

public sealed record CreateFinancialTransactionInput(
    FinancialTransactionType Type,
    decimal Amount,
    DateTime TransactionDate,
    Guid? CashAccountId = null,
    Guid? BankAccountId = null,
    string CurrencyCode = "TRY",
    string? Description = null);

public sealed record FinancialTransactionDto(
    Guid Id,
    Guid? CashAccountId,
    Guid? BankAccountId,
    FinancialTransactionType Type,
    FinancialTransactionDirection Direction,
    decimal Amount,
    decimal BalanceAfter,
    string CurrencyCode,
    DateTime TransactionDate,
    AccountingSourceType SourceType,
    Guid SourceId,
    string? Description,
    Guid? ReversesTransactionId,
    long CreatedBy,
    DateTime CreatedAt);

public sealed record BankTransferInput(
    Guid FromBankAccountId,
    Guid ToBankAccountId,
    decimal Amount,
    DateTime TransactionDate,
    string CurrencyCode = "TRY",
    string? Description = null);

public sealed record BankTransferDto(
    FinancialTransactionDto TransferOut,
    FinancialTransactionDto TransferIn);

public interface IFinancialAccountRepository
{
    // Burada yeni kasa hesabını kalıcı depoya ekliyorum.
    Task AddCashAccountAsync(CashAccount account, CancellationToken cancellationToken = default);
    // Burada yeni banka hesabını kalıcı depoya ekliyorum.
    Task AddBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default);
    // Burada kasa hesabını kimliğiyle takipli getiriyorum.
    Task<CashAccount?> GetCashAccountForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada banka hesabını kimliğiyle takipli getiriyorum.
    Task<BankAccount?> GetBankAccountForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada kasa kodu tekilliğini kontrol ediyorum.
    Task<bool> CashCodeExistsAsync(string code, CancellationToken cancellationToken = default);
    // Burada banka kodu tekilliğini kontrol ediyorum.
    Task<bool> BankCodeExistsAsync(string code, CancellationToken cancellationToken = default);
    // Burada kasa hesaplarını bakiyeleriyle listeliyorum.
    Task<IReadOnlyList<(CashAccount Account, decimal Balance)>> GetCashAccountsAsync(CancellationToken cancellationToken = default);
    // Burada banka hesaplarını bakiyeleriyle listeliyorum.
    Task<IReadOnlyList<(BankAccount Account, decimal Balance)>> GetBankAccountsAsync(CancellationToken cancellationToken = default);
    // Burada kasa ekstresini kronolojik bakiye sonuçlarıyla getiriyorum.
    Task<IReadOnlyList<FinancialTransactionDto>> GetCashStatementAsync(Guid accountId, CancellationToken cancellationToken = default);
    // Burada banka ekstresini kronolojik bakiye sonuçlarıyla getiriyorum.
    Task<IReadOnlyList<FinancialTransactionDto>> GetBankStatementAsync(Guid accountId, CancellationToken cancellationToken = default);
    // Burada yeni finansal ledger hareketini takip etmeye başlıyorum.
    void AddTransaction(FinancialTransaction transaction);
    // Burada aynı kaynak ödeme veya komut için finansal etki oluşup oluşmadığını denetliyorum.
    Task<bool> SourceEffectExistsAsync(
        AccountingSourceType sourceType,
        Guid sourceId,
        CancellationToken cancellationToken = default);
}
