using FluentValidation;

namespace ECommerce.Application.Tags.Queries.GetTagById;

public sealed class GetTagByIdQueryValidator : AbstractValidator<GetTagByIdQuery>
{
    public GetTagByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}
