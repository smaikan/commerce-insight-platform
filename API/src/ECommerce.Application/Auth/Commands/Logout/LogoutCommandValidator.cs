using FluentValidation;

namespace ECommerce.Application.Auth.Commands.Logout;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty();

        RuleFor(command => command.IpAddress)
            .MaximumLength(80);
    }
}
