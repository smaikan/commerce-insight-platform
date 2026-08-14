using ECommerce.Application.Products.Services;
using FluentValidation;

namespace ECommerce.Application.Products.Queries.GetPublishedProductSearchSuggestions;

public sealed class GetPublishedProductSearchSuggestionsQueryValidator
    : AbstractValidator<GetPublishedProductSearchSuggestionsQuery>
{
    // Burada öneri aramasının normalize metin ve sonuç sınırlarını doğruluyorum.
    public GetPublishedProductSearchSuggestionsQueryValidator()
    {
        RuleFor(query => query.Query)
            .NotEmpty()
            .Must(query => ProductSearchTextNormalizer.Normalize(query).Length is >= 2 and <= 100)
            .WithMessage("Query must be between 2 and 100 characters after whitespace normalization.");
        RuleFor(query => query.Limit).InclusiveBetween(1, 10);
    }
}
