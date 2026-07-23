using FluentValidation;

namespace ECommerce.Application.Users.Commands.RevokeSession;

public sealed class RevokeSessionCommandValidator : AbstractValidator<RevokeSessionCommand>
{
    public RevokeSessionCommandValidator()
    {
        RuleFor(command => command.SessionId).NotEmpty();
    }
}
