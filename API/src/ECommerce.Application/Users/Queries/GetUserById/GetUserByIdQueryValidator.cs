using FluentValidation;

namespace ECommerce.Application.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
{
    public GetUserByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}
