using FluentValidation;

namespace ECommerce.Application.Products.Images.Queries.GetProductImageById;

public sealed class GetProductImageByIdQueryValidator : AbstractValidator<GetProductImageByIdQuery>
{
    public GetProductImageByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}
