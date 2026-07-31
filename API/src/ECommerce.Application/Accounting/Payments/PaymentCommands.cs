using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.Accounting.Payments;

public sealed record CreatePaymentCommand(
    string IdempotencyKey,
    CreatePaymentInput Payment) : IRequest<PaymentDto>;

public sealed record GetPaymentByIdQuery(Guid Id) : IRequest<PaymentDto>;

public sealed record GetPaymentsQuery(
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<PaymentSummaryDto>>;

public sealed record CreateCashAccountCommand(FinancialAccountInput Account) : IRequest<CashAccountDto>;
public sealed record CreateBankAccountCommand(BankAccountInput Account) : IRequest<BankAccountDto>;
public sealed record GetCashAccountsQuery : IRequest<IReadOnlyList<CashAccountDto>>;
public sealed record GetBankAccountsQuery : IRequest<IReadOnlyList<BankAccountDto>>;
public sealed record GetCashAccountStatementQuery(Guid AccountId) : IRequest<IReadOnlyList<FinancialTransactionDto>>;
public sealed record GetBankAccountStatementQuery(Guid AccountId) : IRequest<IReadOnlyList<FinancialTransactionDto>>;
public sealed record CreateFinancialTransactionCommand(
    Guid IdempotencySourceId,
    CreateFinancialTransactionInput Transaction) : IRequest<FinancialTransactionDto>;
public sealed record CreateBankTransferCommand(
    Guid IdempotencySourceId,
    BankTransferInput Transfer) : IRequest<BankTransferDto>;
