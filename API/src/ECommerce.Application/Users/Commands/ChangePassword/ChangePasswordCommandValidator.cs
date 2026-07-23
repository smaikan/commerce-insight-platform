using FluentValidation;

namespace ECommerce.Application.Users.Commands.ChangePassword;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(command => command.CurrentPassword).NotEmpty().MaximumLength(128);
        RuleFor(command => command.NewPassword).NotEmpty().MinimumLength(6).MaximumLength(128);
        RuleFor(command => command.NewPassword)
            .NotEqual(command => command.CurrentPassword)
            .WithMessage("New password must be different from the current password.");
    }
}
