using FluentValidation;

namespace ECommerce.Application.Users.Commands.ChangeEmail;

public sealed class ChangeEmailCommandValidator : AbstractValidator<ChangeEmailCommand>
{
    public ChangeEmailCommandValidator()
    {
        RuleFor(command => command.CurrentPassword).NotEmpty().MaximumLength(128);
        RuleFor(command => command.NewEmail).NotEmpty().EmailAddress().MaximumLength(320);
    }
}
