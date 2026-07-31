using ECommerce.Application.Accounting.PurchaseInvoices;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Accounting.CashAndBank;
using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.Payments;
using MediatR;

namespace ECommerce.Application.Accounting.Payments;

public sealed class PaymentHandlers :
    IRequestHandler<CreatePaymentCommand, PaymentDto>,
    IRequestHandler<GetPaymentByIdQuery, PaymentDto>,
    IRequestHandler<GetPaymentsQuery, PagedResult<PaymentSummaryDto>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IFinancialAccountRepository _financialRepository;
    private readonly ICurrentAccountRepository _currentAccountRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    // Burada ödeme, tahsis, cari ve finansal ledger bağımlılıklarını tek işlem için hazırlıyorum.
    public PaymentHandlers(
        IPaymentRepository paymentRepository,
        IFinancialAccountRepository financialRepository,
        ICurrentAccountRepository currentAccountRepository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _financialRepository = financialRepository;
        _currentAccountRepository = currentAccountRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    // Burada tahsilat veya tedarikçi ödemesinin bütün ledger etkilerini atomik ve idempotent kaydediyorum.
    public async Task<PaymentDto> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        var existing = await _paymentRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return Map(existing);
        }

        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            existing = await _paymentRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, transactionToken);
            if (existing is not null)
            {
                return Map(existing);
            }

            var input = request.Payment;
            var account = await _currentAccountRepository.GetByIdForUpdateAsync(input.CurrentAccountId, transactionToken)
                ?? throw new NotFoundException("Current account was not found.");
            ValidateCurrentAccountRole(account, input.Type);
            await ValidateFinancialAccountAsync(input, transactionToken);

            var targetIds = input.Allocations.Select(item => item.CurrentAccountTransactionId).ToArray();
            var targets = await _paymentRepository.GetTransactionsForAllocationAsync(targetIds, transactionToken);
            if (targets.Count != targetIds.Length)
            {
                throw new NotFoundException("One or more current account transactions were not found.");
            }

            foreach (var allocation in input.Allocations)
            {
                var target = targets[allocation.CurrentAccountTransactionId];
                await ValidateAllocationAsync(account, input.Type, target, allocation.Amount, transactionToken);
            }

            var actorId = _currentUser.GetRequiredUserId();
            var payment = new Payment(
                account,
                input.Type,
                input.Amount,
                input.CurrencyCode,
                input.ExchangeRate,
                input.PaymentDate,
                input.CashAccountId,
                input.BankAccountId,
                request.IdempotencyKey,
                actorId,
                input.ReferenceNumber,
                input.Description);

            await _paymentRepository.AddAsync(payment, transactionToken);
            foreach (var inputAllocation in input.Allocations)
            {
                var target = targets[inputAllocation.CurrentAccountTransactionId];
                var allocation = payment.Allocate(
                    target,
                    inputAllocation.Amount);
                _paymentRepository.AddAllocation(allocation);
                var previousPaid = await _paymentRepository.GetValidAllocatedAmountAsync(target.Id, transactionToken);
                await _paymentRepository.SynchronizeSourcePaymentBalanceAsync(
                    target,
                    previousPaid + inputAllocation.Amount,
                    transactionToken);
            }

            var currentAccountEffect = account.AddTransaction(
                input.Type == PaymentType.CustomerCollection
                    ? CurrentAccountTransactionType.CustomerCollection
                    : CurrentAccountTransactionType.SupplierPayment,
                input.Type == PaymentType.SupplierPayment ? input.Amount : 0m,
                input.Type == PaymentType.CustomerCollection ? input.Amount : 0m,
                input.CurrencyCode,
                input.ExchangeRate,
                input.PaymentDate,
                null,
                AccountingSourceType.Payment,
                payment.Id,
                input.Description);
            _currentAccountRepository.AddTransaction(currentAccountEffect);

            var financialEffect = new FinancialTransaction(
                input.CashAccountId,
                input.BankAccountId,
                input.Type == PaymentType.CustomerCollection
                    ? FinancialTransactionType.CustomerCollection
                    : FinancialTransactionType.SupplierPayment,
                input.Amount,
                input.CurrencyCode,
                input.PaymentDate,
                AccountingSourceType.Payment,
                payment.Id,
                actorId,
                input.Description);
            _financialRepository.AddTransaction(financialEffect);

            await _unitOfWork.SaveChangesAsync(transactionToken);
            return Map(payment);
        }, cancellationToken);
    }

    // Burada ödeme detayını geçerli tahsis hedefleriyle getiriyorum.
    public async Task<PaymentDto> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Payment was not found.");
        return Map(payment);
    }

    // Burada ödemeleri kararlı ve sayfalı özetlerle getiriyorum.
    public async Task<PagedResult<PaymentSummaryDto>> Handle(GetPaymentsQuery request, CancellationToken cancellationToken)
    {
        var payments = await _paymentRepository.GetListAsync(request.PageNumber, request.PageSize, cancellationToken);
        return payments.Map(item => new PaymentSummaryDto(
            item.Id,
            item.CurrentAccountId,
            item.Type,
            item.Direction,
            item.Status,
            item.Amount,
            item.CurrencyCode,
            item.PaymentDate));
    }

    // Burada ödeme türünün cari hesap rolüyle uyumunu kontrol ediyorum.
    private static void ValidateCurrentAccountRole(CurrentAccount account, PaymentType type)
    {
        var isAllowed = type == PaymentType.CustomerCollection
            ? account.CanBeCustomer()
            : account.CanBeSupplier();
        if (!isAllowed)
        {
            throw new ConflictException("Current account is inactive or does not support the payment role.");
        }
    }

    // Burada seçilen aktif finans hesabının ödeme para birimiyle uyumlu olduğunu doğruluyorum.
    private async Task ValidateFinancialAccountAsync(CreatePaymentInput input, CancellationToken cancellationToken)
    {
        if (input.CashAccountId.HasValue)
        {
            var cash = await _financialRepository.GetCashAccountForUpdateAsync(input.CashAccountId.Value, cancellationToken)
                ?? throw new NotFoundException("Cash account was not found.");
            if (!cash.IsActive || cash.CurrencyCode != input.CurrencyCode)
            {
                throw new ConflictException("Cash account is inactive or has a different currency.");
            }

            return;
        }

        var bank = await _financialRepository.GetBankAccountForUpdateAsync(input.BankAccountId!.Value, cancellationToken)
            ?? throw new NotFoundException("Bank account was not found.");
        if (!bank.IsActive || bank.CurrencyCode != input.CurrencyCode)
        {
            throw new ConflictException("Bank account is inactive or has a different currency.");
        }
    }

    // Burada tahsis hedefinin rolünü, durumunu ve kalan borç ya da alacak tutarını doğruluyorum.
    private async Task ValidateAllocationAsync(
        CurrentAccount account,
        PaymentType paymentType,
        CurrentAccountTransaction target,
        decimal allocationAmount,
        CancellationToken cancellationToken)
    {
        if (target.CurrentAccountId != account.Id ||
            target.CurrencyCode != "TRY" ||
            await _paymentRepository.IsTransactionReversedAsync(target, cancellationToken))
        {
            throw new ConflictException("Allocation target is incompatible, cancelled or reversed.");
        }

        var expectedType = paymentType == PaymentType.CustomerCollection
            ? CurrentAccountTransactionType.CustomerReceivable
            : CurrentAccountTransactionType.SupplierDebt;
        if (target.Type != expectedType)
        {
            throw new ConflictException("Allocation target does not match the payment type.");
        }

        var originalAmount = paymentType == PaymentType.CustomerCollection
            ? target.DebitAmount
            : target.CreditAmount;
        var paidAmount = await _paymentRepository.GetValidAllocatedAmountAsync(target.Id, cancellationToken);
        if (allocationAmount > originalAmount - paidAmount)
        {
            throw new ConflictException("Allocation exceeds the remaining debt or receivable.");
        }
    }

    // Burada ödeme aggregate'ını geçerli tahsis toplamlarıyla dış sözleşmeye dönüştürüyorum.
    private static PaymentDto Map(Payment payment)
    {
        var allocations = payment.Allocations
            .Where(item => item.IsValid)
            .Select(item => new PaymentAllocationDto(
                item.Id,
                item.CurrentAccountTransactionId,
                item.CurrentAccountTransaction.SourceType,
                item.CurrentAccountTransaction.SourceId,
                item.AllocatedAmount,
                item.IsReversed,
                item.ReversedAt))
            .ToList();
        var allocatedAmount = allocations.Sum(item => item.AllocatedAmount);
        return new PaymentDto(
            payment.Id,
            payment.CurrentAccountId,
            payment.Type,
            payment.Direction,
            payment.Status,
            payment.Amount,
            allocatedAmount,
            payment.Amount - allocatedAmount,
            payment.CurrencyCode,
            payment.PaymentDate,
            payment.CashAccountId,
            payment.BankAccountId,
            payment.ReferenceNumber,
            payment.Description,
            payment.CreatedAt,
            payment.CancelledBy,
            payment.CancelledAt,
            payment.CancellationReason,
            allocations);
    }
}

public sealed class FinancialAccountHandlers :
    IRequestHandler<CreateCashAccountCommand, CashAccountDto>,
    IRequestHandler<CreateBankAccountCommand, BankAccountDto>,
    IRequestHandler<GetCashAccountsQuery, IReadOnlyList<CashAccountDto>>,
    IRequestHandler<GetBankAccountsQuery, IReadOnlyList<BankAccountDto>>,
    IRequestHandler<GetCashAccountStatementQuery, IReadOnlyList<FinancialTransactionDto>>,
    IRequestHandler<GetBankAccountStatementQuery, IReadOnlyList<FinancialTransactionDto>>,
    IRequestHandler<CreateFinancialTransactionCommand, FinancialTransactionDto>,
    IRequestHandler<CreateBankTransferCommand, BankTransferDto>
{
    private readonly IFinancialAccountRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    // Burada kasa, banka ve finansal ledger use case bağımlılıklarını hazırlıyorum.
    public FinancialAccountHandlers(
        IFinancialAccountRepository repository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    // Burada benzersiz kodla yeni kasa hesabı oluşturuyorum.
    public async Task<CashAccountDto> Handle(CreateCashAccountCommand request, CancellationToken cancellationToken)
    {
        if (await _repository.CashCodeExistsAsync(request.Account.Code, cancellationToken))
        {
            throw new ConflictException("Cash account code already exists.");
        }

        var account = new CashAccount(request.Account.Code, request.Account.Name, request.Account.CurrencyCode);
        await _repository.AddCashAccountAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new CashAccountDto(account.Id, account.Code, account.Name, account.CurrencyCode, account.IsActive, 0m);
    }

    // Burada benzersiz kodla yeni banka hesabı oluşturuyorum.
    public async Task<BankAccountDto> Handle(CreateBankAccountCommand request, CancellationToken cancellationToken)
    {
        if (await _repository.BankCodeExistsAsync(request.Account.Code, cancellationToken))
        {
            throw new ConflictException("Bank account code already exists.");
        }

        var account = new BankAccount(
            request.Account.Code,
            request.Account.Name,
            request.Account.BankName,
            request.Account.Iban,
            request.Account.CurrencyCode);
        await _repository.AddBankAccountAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new BankAccountDto(
            account.Id, account.Code, account.Name, account.BankName, account.Iban,
            account.CurrencyCode, account.IsActive, 0m);
    }

    // Burada kasa hesaplarını yalnız finansal hareketlerden türetilen bakiyeleriyle getiriyorum.
    public async Task<IReadOnlyList<CashAccountDto>> Handle(GetCashAccountsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _repository.GetCashAccountsAsync(cancellationToken);
        return accounts.Select(item => new CashAccountDto(
            item.Account.Id, item.Account.Code, item.Account.Name, item.Account.CurrencyCode,
            item.Account.IsActive, item.Balance)).ToList();
    }

    // Burada banka hesaplarını yalnız finansal hareketlerden türetilen bakiyeleriyle getiriyorum.
    public async Task<IReadOnlyList<BankAccountDto>> Handle(GetBankAccountsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _repository.GetBankAccountsAsync(cancellationToken);
        return accounts.Select(item => new BankAccountDto(
            item.Account.Id, item.Account.Code, item.Account.Name, item.Account.BankName,
            item.Account.Iban, item.Account.CurrencyCode, item.Account.IsActive, item.Balance)).ToList();
    }

    // Burada kasa hesabının hareket bazlı ekstresini getiriyorum.
    public Task<IReadOnlyList<FinancialTransactionDto>> Handle(
        GetCashAccountStatementQuery request,
        CancellationToken cancellationToken)
    {
        return _repository.GetCashStatementAsync(request.AccountId, cancellationToken);
    }

    // Burada banka hesabının hareket bazlı ekstresini getiriyorum.
    public Task<IReadOnlyList<FinancialTransactionDto>> Handle(
        GetBankAccountStatementQuery request,
        CancellationToken cancellationToken)
    {
        return _repository.GetBankStatementAsync(request.AccountId, cancellationToken);
    }

    // Burada onaylı manuel kasa veya banka hareketini idempotent ledger kaydı olarak oluşturuyorum.
    public async Task<FinancialTransactionDto> Handle(
        CreateFinancialTransactionCommand request,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            if (await _repository.SourceEffectExistsAsync(
                    AccountingSourceType.FinancialTransaction,
                    request.IdempotencySourceId,
                    transactionToken))
            {
                throw new ConflictException("Financial transaction source already exists.");
            }

            var input = request.Transaction;
            await ValidateAccountAsync(input, transactionToken);
            var transaction = new FinancialTransaction(
                input.CashAccountId,
                input.BankAccountId,
                input.Type,
                input.Amount,
                input.CurrencyCode,
                input.TransactionDate,
                AccountingSourceType.FinancialTransaction,
                request.IdempotencySourceId,
                _currentUser.GetRequiredUserId(),
                input.Description);
            _repository.AddTransaction(transaction);
            await _unitOfWork.SaveChangesAsync(transactionToken);

            var statement = input.CashAccountId.HasValue
                ? await _repository.GetCashStatementAsync(input.CashAccountId.Value, transactionToken)
                : await _repository.GetBankStatementAsync(input.BankAccountId!.Value, transactionToken);
            return statement.Single(item => item.Id == transaction.Id);
        }, cancellationToken);
    }

    // Burada iki banka arasındaki çıkış ve giriş hareketlerini tek transaction içinde atomik oluşturuyorum.
    public async Task<BankTransferDto> Handle(
        CreateBankTransferCommand request,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionToken =>
        {
            if (await _repository.SourceEffectExistsAsync(
                    AccountingSourceType.FinancialTransaction,
                    request.IdempotencySourceId,
                    transactionToken))
            {
                throw new ConflictException("Bank transfer source already exists.");
            }

            var input = request.Transfer;
            var from = await _repository.GetBankAccountForUpdateAsync(input.FromBankAccountId, transactionToken)
                ?? throw new NotFoundException("Source bank account was not found.");
            var to = await _repository.GetBankAccountForUpdateAsync(input.ToBankAccountId, transactionToken)
                ?? throw new NotFoundException("Destination bank account was not found.");
            if (!from.IsActive || !to.IsActive ||
                from.CurrencyCode != input.CurrencyCode ||
                to.CurrencyCode != input.CurrencyCode)
            {
                throw new ConflictException("Transfer accounts must be active and use the transfer currency.");
            }

            var actorId = _currentUser.GetRequiredUserId();
            var transferOut = new FinancialTransaction(
                null, from.Id, FinancialTransactionType.BankTransferOut, input.Amount,
                input.CurrencyCode, input.TransactionDate, AccountingSourceType.FinancialTransaction,
                request.IdempotencySourceId, actorId, input.Description);
            var transferIn = new FinancialTransaction(
                null, to.Id, FinancialTransactionType.BankTransferIn, input.Amount,
                input.CurrencyCode, input.TransactionDate, AccountingSourceType.FinancialTransaction,
                request.IdempotencySourceId, actorId, input.Description);
            _repository.AddTransaction(transferOut);
            _repository.AddTransaction(transferIn);
            await _unitOfWork.SaveChangesAsync(transactionToken);

            var outStatement = await _repository.GetBankStatementAsync(from.Id, transactionToken);
            var inStatement = await _repository.GetBankStatementAsync(to.Id, transactionToken);
            return new BankTransferDto(
                outStatement.Single(item => item.Id == transferOut.Id),
                inStatement.Single(item => item.Id == transferIn.Id));
        }, cancellationToken);
    }

    // Burada manuel finansal hareketin aktif ve aynı para birimli hesapta olduğunu doğruluyorum.
    private async Task ValidateAccountAsync(CreateFinancialTransactionInput input, CancellationToken cancellationToken)
    {
        if (input.CashAccountId.HasValue)
        {
            var cash = await _repository.GetCashAccountForUpdateAsync(input.CashAccountId.Value, cancellationToken)
                ?? throw new NotFoundException("Cash account was not found.");
            if (!cash.IsActive || cash.CurrencyCode != input.CurrencyCode)
            {
                throw new ConflictException("Cash account is inactive or has a different currency.");
            }

            return;
        }

        var bank = await _repository.GetBankAccountForUpdateAsync(input.BankAccountId!.Value, cancellationToken)
            ?? throw new NotFoundException("Bank account was not found.");
        if (!bank.IsActive || bank.CurrencyCode != input.CurrencyCode)
        {
            throw new ConflictException("Bank account is inactive or has a different currency.");
        }
    }
}
