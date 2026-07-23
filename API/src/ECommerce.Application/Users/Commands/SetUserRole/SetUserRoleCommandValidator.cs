using FluentValidation;

namespace ECommerce.Application.Users.Commands.SetUserRole;

public sealed class SetUserRoleCommandValidator : AbstractValidator<SetUserRoleCommand>
{
    public SetUserRoleCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Role).IsInEnum();
    }
}
