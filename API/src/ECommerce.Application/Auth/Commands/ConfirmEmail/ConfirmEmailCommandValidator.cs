using FluentValidation;

namespace ECommerce.Application.Auth.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(command => command.Token)
            .NotEmpty();
    }
}
