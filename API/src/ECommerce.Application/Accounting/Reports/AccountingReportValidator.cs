using FluentValidation;

namespace ECommerce.Application.Accounting.Reports;

public sealed class GetAccountingReportQueryValidator : AbstractValidator<GetAccountingReportQuery>
{
    public GetAccountingReportQueryValidator()
    {
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 10_000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x).Must(x => !x.From.HasValue || !x.To.HasValue || x.From <= x.To)
            .WithMessage("From date cannot be later than to date.");
    }
}
