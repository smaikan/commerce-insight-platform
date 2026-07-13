using FluentValidation;

namespace ECommerce.Application.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.Token)
            .NotEmpty();

        RuleFor(command => command.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128);
    }
}
