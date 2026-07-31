using FluentValidation;

namespace ECommerce.Application.Returns.Queries.GetReturnRequests;

public sealed class GetReturnRequestsQueryValidator : AbstractValidator<GetReturnRequestsQuery>
{
    // Burada yönetim iade listesi için sayfa, kimlik, enum ve tarih sınırlarını doğruluyorum.
    public GetReturnRequestsQueryValidator()
    {
        RuleFor(query => query.PageNumber).InclusiveBetween(1, 10_000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.OrderId).NotEmpty().When(query => query.OrderId.HasValue);
        RuleFor(query => query.Type).IsInEnum().When(query => query.Type.HasValue);
        RuleFor(query => query.Status).IsInEnum().When(query => query.Status.HasValue);
        RuleFor(query => query)
            .Must(query => !query.CreatedFromUtc.HasValue || !query.CreatedToUtc.HasValue || query.CreatedFromUtc <= query.CreatedToUtc)
            .WithMessage("CreatedFromUtc cannot be later than CreatedToUtc.");
    }
}
