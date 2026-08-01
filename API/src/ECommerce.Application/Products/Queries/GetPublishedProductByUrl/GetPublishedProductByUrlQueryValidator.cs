using FluentValidation;

namespace ECommerce.Application.Products.Queries.GetPublishedProductByUrl;

public sealed class GetPublishedProductByUrlQueryValidator : AbstractValidator<GetPublishedProductByUrlQuery>
{
    public GetPublishedProductByUrlQueryValidator()
    {
        RuleFor(query => query.Url).NotEmpty().MaximumLength(250);
    }
}
