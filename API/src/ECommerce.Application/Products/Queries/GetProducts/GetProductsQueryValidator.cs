using FluentValidation;

namespace ECommerce.Application.Products.Queries.GetProducts;

public sealed class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    // Burada admin ürün listeleme parametrelerinin güvenli sınırlarını doğruluyorum.
    public GetProductsQueryValidator()
    {
        RuleFor(query => query.PageNumber).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Search).MaximumLength(250);
        RuleFor(query => query.TypeId).Must(BeNullOrNonEmptyGuid);
        RuleFor(query => query.BrandId).Must(BeNullOrNonEmptyGuid);
        RuleFor(query => query.CollectionId).Must(BeNullOrNonEmptyGuid);
        RuleFor(query => query.TagId).Must(BeNullOrNonEmptyGuid);
        RuleFor(query => query.SortBy).IsInEnum();
    }

    // Burada opsiyonel filtre kimliklerinin boş GUID olmamasını sağlıyorum.
    private static bool BeNullOrNonEmptyGuid(Guid? id) => !id.HasValue || id.Value != Guid.Empty;
}
