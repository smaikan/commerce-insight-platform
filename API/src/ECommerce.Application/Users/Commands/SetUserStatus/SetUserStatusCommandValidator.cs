using FluentValidation;

namespace ECommerce.Application.Users.Commands.SetUserStatus;

public sealed class SetUserStatusCommandValidator : AbstractValidator<SetUserStatusCommand>
{
    public SetUserStatusCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Status).IsInEnum();
    }
}
