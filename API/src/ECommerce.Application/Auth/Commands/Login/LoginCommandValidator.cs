using FluentValidation;

namespace ECommerce.Application.Auth.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(command => command.IpAddress)
            .MaximumLength(80);
    }
}
