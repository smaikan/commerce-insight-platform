using FluentValidation;

namespace ECommerce.Application.StockMovements.Queries.GetStockMovements;

public sealed class GetStockMovementsQueryValidator : AbstractValidator<GetStockMovementsQuery>
{
    // Burada stok hareketi sorgusunun kimlik, enum, tarih ve sayfalama sınırlarını doğruluyorum.
    public GetStockMovementsQueryValidator()
    {
        RuleFor(query => query.PageNumber).InclusiveBetween(1, 10_000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.ProductVariantId)
            .NotEqual(Guid.Empty)
            .When(query => query.ProductVariantId.HasValue);
        RuleFor(query => query.Direction)
            .IsInEnum()
            .When(query => query.Direction.HasValue);
        RuleFor(query => query.Type)
            .IsInEnum()
            .When(query => query.Type.HasValue);
        RuleFor(query => query)
            .Must(query =>
                !query.CreatedFromUtc.HasValue ||
                !query.CreatedToUtc.HasValue ||
                query.CreatedFromUtc <= query.CreatedToUtc)
            .WithMessage("CreatedFromUtc cannot be later than CreatedToUtc.");
    }
}
