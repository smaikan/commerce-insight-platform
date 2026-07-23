using FluentValidation;

namespace ECommerce.Application.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty();

        RuleFor(command => command.IpAddress)
            .MaximumLength(80);

        RuleFor(command => command.DeviceName)
            .MaximumLength(200);
    }
}
