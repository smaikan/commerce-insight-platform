using FluentValidation;
using ECommerce.Application.Products.Services;

namespace ECommerce.Application.Products.Queries.GetPublishedProducts;

public sealed class GetPublishedProductsQueryValidator : AbstractValidator<GetPublishedProductsQuery>
{
    // Burada storefront sayfalama ve sıralama değerlerini güvenli aralıkta doğruluyorum.
    public GetPublishedProductsQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.TypeId).Must(BeNullOrNonEmptyGuid);
        RuleFor(query => query.BrandId).Must(BeNullOrNonEmptyGuid);
        RuleFor(query => query.CollectionId).Must(BeNullOrNonEmptyGuid);
        RuleFor(query => query.TagId).Must(BeNullOrNonEmptyGuid);
        RuleFor(query => query.SortBy).IsInEnum().When(query => query.SortBy.HasValue);
        RuleFor(query => query.Search)
            .Must(search => string.IsNullOrWhiteSpace(search) ||
                ProductSearchTextNormalizer.Normalize(search).Length is >= 2 and <= 100)
            .WithMessage("Search must be between 2 and 100 characters after whitespace normalization.");
    }

    // Burada opsiyonel storefront filtre kimliklerinin boş GUID olmamasını sağlıyorum.
    private static bool BeNullOrNonEmptyGuid(Guid? id) => !id.HasValue || id.Value != Guid.Empty;
}
