using FluentValidation;

namespace ECommerce.Application.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    // Burada refresh tokenı zorunlu tutarken yeni oturumun cihaz adını isteğe bağlı bırakıyorum.
    public RefreshTokenCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty();

        RuleFor(command => command.IpAddress)
            .MaximumLength(80);

        RuleFor(command => command.DeviceName)
            .MaximumLength(200)
            .When(command => command.DeviceName is not null);
    }
}
