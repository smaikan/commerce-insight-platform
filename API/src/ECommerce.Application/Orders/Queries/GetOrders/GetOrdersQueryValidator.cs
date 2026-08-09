using FluentValidation;

namespace ECommerce.Application.Orders.Queries.GetOrders;

public sealed class GetOrdersQueryValidator : AbstractValidator<GetOrdersQuery>
{
    // Burada yönetim sipariş listesinin sayfa ve tarih sınırlarını performans için doğruluyorum.
    public GetOrdersQueryValidator()
    {
        RuleFor(query => query.PageNumber).InclusiveBetween(1, 10_000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Search).MaximumLength(100);
        RuleFor(query => query.Status).IsInEnum().When(query => query.Status.HasValue);
        RuleFor(query => query)
            .Must(query => !query.CreatedFromUtc.HasValue || !query.CreatedToUtc.HasValue || query.CreatedFromUtc <= query.CreatedToUtc)
            .WithMessage("CreatedFromUtc cannot be later than CreatedToUtc.");
    }
}
