using FluentValidation;

namespace ECommerce.Application.Users.Commands.CloseAccount;

public sealed class CloseAccountCommandValidator : AbstractValidator<CloseAccountCommand>
{
    public CloseAccountCommandValidator()
    {
        RuleFor(command => command.CurrentPassword).NotEmpty().MaximumLength(128);
    }
}
