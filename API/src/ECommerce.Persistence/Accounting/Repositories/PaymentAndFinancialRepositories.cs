using ECommerce.Application.Accounting.Payments;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Accounting.CashAndBank;
using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.Payments;
using ECommerce.Domain.Accounting.PurchaseInvoices;
using ECommerce.Domain.Accounting.SalesOrders;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Accounting.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _context;

    // Burada ödeme repository'sini aynı request DbContext'iyle hazırlıyorum.
    public PaymentRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni muhasebe ödemesini takip etmeye başlıyorum.
    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await _context.Set<Payment>().AddAsync(payment, cancellationToken);
    }

    // Burada ödeme detayını tahsis ve cari hedefleriyle takip etmeden getiriyorum.
    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return PaymentGraph().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task<Payment?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Set<Payment>()
            .Include(item => item.Allocations)
            .ThenInclude(item => item.CurrentAccountTransaction)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    // Burada istemci tekrarında mevcut ödeme ve tahsislerini getiriyorum.
    public Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var normalized = idempotencyKey.Trim();
        return PaymentGraph().FirstOrDefaultAsync(item => item.IdempotencyKey == normalized, cancellationToken);
    }

    // Burada ödemeleri tarih ve kimlik sırasıyla sayfalıyorum.
    public async Task<PagedResult<Payment>> GetListAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Payment>().AsNoTracking()
            .OrderByDescending(item => item.PaymentDate)
            .ThenByDescending(item => item.Id);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<Payment>(items, pageNumber, pageSize, totalCount);
    }

    // Burada seçili cari hareketleri tahsis doğrulaması için takipli getiriyorum.
    public async Task<IReadOnlyDictionary<Guid, CurrentAccountTransaction>> GetTransactionsForAllocationAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var values = ids.Distinct().ToArray();
        return await _context.Set<CurrentAccountTransaction>()
            .Where(item => values.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
    }

    // Burada cari hareketin yalnız tamamlanmış ve terslenmemiş ödeme tahsislerini topluyorum.
    public Task<decimal> GetValidAllocatedAmountAsync(
        Guid currentAccountTransactionId,
        CancellationToken cancellationToken = default)
    {
        return _context.Set<PaymentAllocation>()
            .Where(item =>
                item.CurrentAccountTransactionId == currentAccountTransactionId &&
                !item.IsReversed &&
                item.Payment.Status == PaymentStatus.Completed)
            .SumAsync(item => item.AllocatedAmount, cancellationToken);
    }

    // Burada borç veya alacak kaynağı için ters cari hareket oluşmuşsa yeni tahsisi engelliyorum.
    public Task<bool> IsTransactionReversedAsync(
        CurrentAccountTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        var reversalType = transaction.Type switch
        {
            CurrentAccountTransactionType.SupplierDebt => CurrentAccountTransactionType.SupplierDebtReversal,
            CurrentAccountTransactionType.CustomerReceivable => CurrentAccountTransactionType.CustomerReceivableReversal,
            _ => transaction.Type
        };
        return _context.Set<CurrentAccountTransaction>().AnyAsync(item =>
            item.SourceType == transaction.SourceType &&
            item.SourceId == transaction.SourceId &&
            item.Type == reversalType,
            cancellationToken);
    }

    // Burada PurchaseInvoice veya AccountingSalesOrder gösterim bakiyesini tahsis ledger toplamıyla eşitliyorum.
    public async Task SynchronizeSourcePaymentBalanceAsync(
        CurrentAccountTransaction transaction,
        decimal paidAmount,
        CancellationToken cancellationToken = default)
    {
        if (transaction.SourceType == AccountingSourceType.PurchaseInvoice)
        {
            var invoice = await _context.Set<PurchaseInvoice>()
                .FirstOrDefaultAsync(item => item.Id == transaction.SourceId, cancellationToken)
                ?? throw new InvalidOperationException("Purchase invoice payment source was not found.");
            invoice.SynchronizePaymentBalance(paidAmount);
            return;
        }

        if (transaction.SourceType == AccountingSourceType.AccountingSalesOrder)
        {
            var order = await _context.Set<AccountingSalesOrder>()
                .Include(item => item.SalesInvoice)
                .FirstOrDefaultAsync(item => item.Id == transaction.SourceId, cancellationToken)
                ?? throw new InvalidOperationException("Accounting sales order payment source was not found.");
            order.SynchronizePaymentBalance(paidAmount);
            return;
        }

        throw new InvalidOperationException("Current account transaction is not an allocatable document source.");
    }

    // Burada aggregate üzerinden eklenen tahsisin EF durumunu kesin olarak Added yapıyorum.
    public void AddAllocation(PaymentAllocation allocation)
    {
        _context.Set<PaymentAllocation>().Add(allocation);
    }

    // Burada ödeme detay sorgularının ortak salt okunur graph'ını kuruyorum.
    private IQueryable<Payment> PaymentGraph()
    {
        return _context.Set<Payment>().AsNoTracking()
            .Include(item => item.Allocations)
            .ThenInclude(item => item.CurrentAccountTransaction);
    }
}

public sealed class FinancialAccountRepository : IFinancialAccountRepository
{
    private readonly AppDbContext _context;

    // Burada finans hesabı repository'sini aynı request DbContext'iyle hazırlıyorum.
    public FinancialAccountRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni kasa hesabını takip etmeye başlıyorum.
    public async Task AddCashAccountAsync(CashAccount account, CancellationToken cancellationToken = default)
    {
        await _context.Set<CashAccount>().AddAsync(account, cancellationToken);
    }

    // Burada yeni banka hesabını takip etmeye başlıyorum.
    public async Task AddBankAccountAsync(BankAccount account, CancellationToken cancellationToken = default)
    {
        await _context.Set<BankAccount>().AddAsync(account, cancellationToken);
    }

    // Burada kasa hesabını finansal işlem doğrulaması için takipli getiriyorum.
    public Task<CashAccount?> GetCashAccountForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Set<CashAccount>().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    // Burada banka hesabını finansal işlem doğrulaması için takipli getiriyorum.
    public Task<BankAccount?> GetBankAccountForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Set<BankAccount>().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    // Burada normalize kasa kodu tekilliğini kontrol ediyorum.
    public Task<bool> CashCodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return _context.Set<CashAccount>().AnyAsync(item => item.Code == normalized, cancellationToken);
    }

    // Burada normalize banka kodu tekilliğini kontrol ediyorum.
    public Task<bool> BankCodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return _context.Set<BankAccount>().AnyAsync(item => item.Code == normalized, cancellationToken);
    }

    // Burada kasa hesaplarını finansal ledger toplamından türetilen bakiyeleriyle getiriyorum.
    public async Task<IReadOnlyList<(CashAccount Account, decimal Balance)>> GetCashAccountsAsync(
        CancellationToken cancellationToken = default)
    {
        var accounts = await _context.Set<CashAccount>().AsNoTracking().OrderBy(item => item.Code).ToListAsync(cancellationToken);
        var balances = await _context.Set<FinancialTransaction>()
            .Where(item => item.CashAccountId.HasValue)
            .GroupBy(item => item.CashAccountId!.Value)
            .Select(group => new
            {
                Id = group.Key,
                Balance = group.Sum(item =>
                    item.Direction == FinancialTransactionDirection.In ? item.Amount : -item.Amount)
            })
            .ToDictionaryAsync(item => item.Id, item => item.Balance, cancellationToken);
        return accounts.Select(item => (item, balances.GetValueOrDefault(item.Id))).ToList();
    }

    // Burada banka hesaplarını finansal ledger toplamından türetilen bakiyeleriyle getiriyorum.
    public async Task<IReadOnlyList<(BankAccount Account, decimal Balance)>> GetBankAccountsAsync(
        CancellationToken cancellationToken = default)
    {
        var accounts = await _context.Set<BankAccount>().AsNoTracking().OrderBy(item => item.Code).ToListAsync(cancellationToken);
        var balances = await _context.Set<FinancialTransaction>()
            .Where(item => item.BankAccountId.HasValue)
            .GroupBy(item => item.BankAccountId!.Value)
            .Select(group => new
            {
                Id = group.Key,
                Balance = group.Sum(item =>
                    item.Direction == FinancialTransactionDirection.In ? item.Amount : -item.Amount)
            })
            .ToDictionaryAsync(item => item.Id, item => item.Balance, cancellationToken);
        return accounts.Select(item => (item, balances.GetValueOrDefault(item.Id))).ToList();
    }

    // Burada kasa ekstresini hareketlerden kümülatif bakiye üreterek getiriyorum.
    public async Task<IReadOnlyList<FinancialTransactionDto>> GetCashStatementAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var transactions = await _context.Set<FinancialTransaction>().AsNoTracking()
            .Where(item => item.CashAccountId == accountId)
            .OrderBy(item => item.TransactionDate)
            .ThenBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
        return MapStatement(transactions);
    }

    // Burada banka ekstresini hareketlerden kümülatif bakiye üreterek getiriyorum.
    public async Task<IReadOnlyList<FinancialTransactionDto>> GetBankStatementAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var transactions = await _context.Set<FinancialTransaction>().AsNoTracking()
            .Where(item => item.BankAccountId == accountId)
            .OrderBy(item => item.TransactionDate)
            .ThenBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
        return MapStatement(transactions);
    }

    // Burada yeni finansal hareketi kesin Added durumunda izliyorum.
    public void AddTransaction(FinancialTransaction transaction)
    {
        _context.Set<FinancialTransaction>().Add(transaction);
    }

    // Burada kaynak başına tek finansal etki kuralını doğruluyorum.
    public Task<bool> SourceEffectExistsAsync(
        AccountingSourceType sourceType,
        Guid sourceId,
        CancellationToken cancellationToken = default)
    {
        return _context.Set<FinancialTransaction>().AnyAsync(item =>
            item.SourceType == sourceType && item.SourceId == sourceId,
            cancellationToken);
    }

    // Burada sıralı finansal hareketleri kümülatif bakiye taşıyan ekstre satırlarına dönüştürüyorum.
    private static IReadOnlyList<FinancialTransactionDto> MapStatement(
        IReadOnlyList<FinancialTransaction> transactions)
    {
        var balance = 0m;
        return transactions.Select(item =>
        {
            balance += item.Direction == FinancialTransactionDirection.In ? item.Amount : -item.Amount;
            return new FinancialTransactionDto(
                item.Id,
                item.CashAccountId,
                item.BankAccountId,
                item.Type,
                item.Direction,
                item.Amount,
                balance,
                item.CurrencyCode,
                item.TransactionDate,
                item.SourceType,
                item.SourceId,
                item.Description,
                item.ReversesTransactionId,
                item.CreatedBy,
                item.CreatedAt);
        }).ToList();
    }
}
