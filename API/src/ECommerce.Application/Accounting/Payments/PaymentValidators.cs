using ECommerce.Domain.Accounting.CashAndBank;
using ECommerce.Domain.Accounting.Payments;
using FluentValidation;

namespace ECommerce.Application.Accounting.Payments;

public sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    // Burada ödeme, finans hesabı ve tahsis listesinin sınırlarını doğruluyorum.
    public CreatePaymentCommandValidator()
    {
        RuleFor(command => command.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(Payment.MaximumIdempotencyKeyLength)
            .Matches("^[A-Za-z0-9_-]+$");
        RuleFor(command => command.Payment).NotNull();
        RuleFor(command => command.Payment.CurrentAccountId).NotEmpty();
        RuleFor(command => command.Payment.Type).IsInEnum();
        RuleFor(command => command.Payment.Amount).GreaterThan(0m);
        RuleFor(command => command.Payment.PaymentDate).NotEmpty();
        RuleFor(command => command.Payment.CurrencyCode).Equal("TRY").WithMessage("Currency code must be TRY.");
        RuleFor(command => command.Payment.ExchangeRate).Equal(1m);
        RuleFor(command => command.Payment.Allocations).NotNull();
        RuleFor(command => command.Payment.Allocations)
            .NotEmpty()
            .When(command => command.Payment.Type == PaymentType.CustomerCollection)
            .WithMessage("Customer collections must contain at least one allocation.");
        RuleForEach(command => command.Payment.Allocations).ChildRules(allocation =>
        {
            allocation.RuleFor(item => item.CurrentAccountTransactionId).NotEmpty();
            allocation.RuleFor(item => item.Amount).GreaterThan(0m);
        });
        RuleFor(command => command.Payment)
            .Must(input => (input.CashAccountId.HasValue ? 1 : 0) + (input.BankAccountId.HasValue ? 1 : 0) == 1)
            .WithMessage("Exactly one cash or bank account is required.");
        RuleFor(command => command.Payment.Allocations)
            .Must(items => items.Select(item => item.CurrentAccountTransactionId).Distinct().Count() == items.Count)
            .When(command => command.Payment.Allocations is not null)
            .WithMessage("Allocation targets must be unique.");
        RuleFor(command => command.Payment)
            .Must(input => input.Type == PaymentType.SupplierPayment && input.Allocations.Count == 0 ||
                           input.Allocations.Sum(item => item.Amount) == input.Amount)
            .When(command => command.Payment.Allocations is not null)
            .WithMessage("Payment amount must equal allocation total unless this is an unallocated supplier advance.");
    }
}

public sealed class GetPaymentsQueryValidator : AbstractValidator<GetPaymentsQuery>
{
    // Burada ödeme listesinin güvenli sayfalama sınırlarını doğruluyorum.
    public GetPaymentsQueryValidator()
    {
        RuleFor(query => query.PageNumber).InclusiveBetween(1, 10_000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class FinancialAccountInputValidator : AbstractValidator<FinancialAccountInput>
{
    // Burada kasa hesabı girdisinin kod, ad ve TRY sözleşmesini doğruluyorum.
    public FinancialAccountInputValidator()
    {
        RuleFor(input => input.Code).NotEmpty().MaximumLength(CashAccount.MaximumCodeLength);
        RuleFor(input => input.Name).NotEmpty().MaximumLength(CashAccount.MaximumNameLength);
        RuleFor(input => input.CurrencyCode).Equal("TRY");
    }
}

public sealed class BankAccountInputValidator : AbstractValidator<BankAccountInput>
{
    // Burada banka hesabı girdisinin kimlik ve TRY sözleşmesini doğruluyorum.
    public BankAccountInputValidator()
    {
        RuleFor(input => input.Code).NotEmpty().MaximumLength(BankAccount.MaximumCodeLength);
        RuleFor(input => input.Name).NotEmpty().MaximumLength(BankAccount.MaximumNameLength);
        RuleFor(input => input.BankName).NotEmpty().MaximumLength(BankAccount.MaximumBankNameLength);
        RuleFor(input => input.Iban).MaximumLength(BankAccount.MaximumIbanLength);
        RuleFor(input => input.CurrencyCode).Equal("TRY");
    }
}

public sealed class CreateCashAccountCommandValidator : AbstractValidator<CreateCashAccountCommand>
{
    // Burada kasa hesabı oluşturma komutunun girdisini doğruluyorum.
    public CreateCashAccountCommandValidator()
    {
        RuleFor(command => command.Account).NotNull().SetValidator(new FinancialAccountInputValidator());
    }
}

public sealed class CreateBankAccountCommandValidator : AbstractValidator<CreateBankAccountCommand>
{
    // Burada banka hesabı oluşturma komutunun girdisini doğruluyorum.
    public CreateBankAccountCommandValidator()
    {
        RuleFor(command => command.Account).NotNull().SetValidator(new BankAccountInputValidator());
    }
}

public sealed class CreateFinancialTransactionCommandValidator : AbstractValidator<CreateFinancialTransactionCommand>
{
    private static readonly FinancialTransactionType[] AllowedTypes =
    [
        FinancialTransactionType.CashIn,
        FinancialTransactionType.CashOut,
        FinancialTransactionType.BankTransferIn,
        FinancialTransactionType.BankTransferOut,
        FinancialTransactionType.BankCommission,
        FinancialTransactionType.MarketplaceCommission,
        FinancialTransactionType.Refund
    ];

    // Burada manuel finansal hareketin yalnız onaylı tip ve tek hesap kullanmasını doğruluyorum.
    public CreateFinancialTransactionCommandValidator()
    {
        RuleFor(command => command.IdempotencySourceId).NotEmpty();
        RuleFor(command => command.Transaction.Type).Must(type => AllowedTypes.Contains(type));
        RuleFor(command => command.Transaction.Amount).GreaterThan(0m);
        RuleFor(command => command.Transaction.TransactionDate).NotEmpty();
        RuleFor(command => command.Transaction.CurrencyCode).Equal("TRY");
        RuleFor(command => command.Transaction)
            .Must(input => (input.CashAccountId.HasValue ? 1 : 0) + (input.BankAccountId.HasValue ? 1 : 0) == 1)
            .WithMessage("Exactly one cash or bank account is required.");
        RuleFor(command => command.Transaction)
            .Must(IsCompatibleAccount)
            .WithMessage("Financial transaction type is not compatible with the selected account.");
    }

    // Burada kasa ve banka hareket türlerinin doğru hesap türünde kullanılmasını denetliyorum.
    private static bool IsCompatibleAccount(CreateFinancialTransactionInput input)
    {
        return input.Type switch
        {
            FinancialTransactionType.CashIn or FinancialTransactionType.CashOut => input.CashAccountId.HasValue,
            FinancialTransactionType.BankTransferIn or FinancialTransactionType.BankTransferOut or
                FinancialTransactionType.BankCommission or FinancialTransactionType.MarketplaceCommission =>
                input.BankAccountId.HasValue,
            FinancialTransactionType.Refund => true,
            _ => false
        };
    }
}

public sealed class CreateBankTransferCommandValidator : AbstractValidator<CreateBankTransferCommand>
{
    // Burada banka transferinin farklı hesaplar, pozitif tutar ve TRY ile yapılmasını doğruluyorum.
    public CreateBankTransferCommandValidator()
    {
        RuleFor(command => command.IdempotencySourceId).NotEmpty();
        RuleFor(command => command.Transfer.FromBankAccountId).NotEmpty();
        RuleFor(command => command.Transfer.ToBankAccountId).NotEmpty();
        RuleFor(command => command.Transfer)
            .Must(input => input.FromBankAccountId != input.ToBankAccountId)
            .WithMessage("Transfer bank accounts must be different.");
        RuleFor(command => command.Transfer.Amount).GreaterThan(0m);
        RuleFor(command => command.Transfer.TransactionDate).NotEmpty();
        RuleFor(command => command.Transfer.CurrencyCode).Equal("TRY");
    }
}
