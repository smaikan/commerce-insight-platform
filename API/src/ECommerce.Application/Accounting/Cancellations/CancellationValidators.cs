using FluentValidation;

namespace ECommerce.Application.Accounting.Cancellations;

public class CancellationReasonValidator<T> : AbstractValidator<T> where T : class
{
    public CancellationReasonValidator(Func<T, Guid> id, Func<T, string> reason)
    {
        RuleFor(x => id(x)).NotEmpty();
        RuleFor(x => reason(x)).NotEmpty().MaximumLength(500);
    }
}

public sealed class CancelAccountingSalesOrderCommandValidator : CancellationReasonValidator<CancelAccountingSalesOrderCommand>
{ public CancelAccountingSalesOrderCommandValidator() : base(x => x.Id, x => x.Reason) { } }
public sealed class CancelPurchaseInvoiceCommandValidator : CancellationReasonValidator<CancelPurchaseInvoiceCommand>
{ public CancelPurchaseInvoiceCommandValidator() : base(x => x.Id, x => x.Reason) { } }
public sealed class CancelSalesInvoiceCommandValidator : CancellationReasonValidator<CancelSalesInvoiceCommand>
{ public CancelSalesInvoiceCommandValidator() : base(x => x.Id, x => x.Reason) { } }
public sealed class CancelPaymentCommandValidator : CancellationReasonValidator<CancelPaymentCommand>
{ public CancelPaymentCommandValidator() : base(x => x.Id, x => x.Reason) { } }
public sealed class ReverseFinancialTransactionCommandValidator : CancellationReasonValidator<ReverseFinancialTransactionCommand>
{ public ReverseFinancialTransactionCommandValidator() : base(x => x.Id, x => x.Reason) { } }
