using FluentValidation;

namespace ECommerce.Application.Products.Queries.GetPublishedProductFacets;

public sealed class GetPublishedProductFacetsQueryValidator : AbstractValidator<GetPublishedProductFacetsQuery>
{
    // Burada facet boyutunu ve opsiyonel sınıflandırma kimliklerini doğruluyorum.
    public GetPublishedProductFacetsQueryValidator()
    {
        RuleFor(query => query.Dimension).IsInEnum();
        RuleFor(query => query.TypeId).Must(BeNullOrNonEmptyGuid);
        RuleFor(query => query.BrandId).Must(BeNullOrNonEmptyGuid);
        RuleFor(query => query.CollectionId).Must(BeNullOrNonEmptyGuid);
        RuleFor(query => query.TagId).Must(BeNullOrNonEmptyGuid);
    }

    // Burada opsiyonel facet filtre kimliklerinin boş GUID olmamasını sağlıyorum.
    private static bool BeNullOrNonEmptyGuid(Guid? id) => !id.HasValue || id.Value != Guid.Empty;
}
