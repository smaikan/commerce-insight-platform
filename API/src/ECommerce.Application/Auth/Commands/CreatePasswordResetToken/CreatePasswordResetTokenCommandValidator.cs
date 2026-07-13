using FluentValidation;

namespace ECommerce.Application.Auth.Commands.CreatePasswordResetToken;

public sealed class CreatePasswordResetTokenCommandValidator : AbstractValidator<CreatePasswordResetTokenCommand>
{
    public CreatePasswordResetTokenCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);
    }
}
