using FluentValidation;

namespace ECommerce.Application.Collections.Queries.GetCollectionById;

public sealed class GetCollectionByIdQueryValidator : AbstractValidator<GetCollectionByIdQuery>
{
    public GetCollectionByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}
