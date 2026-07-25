using FluentValidation;

namespace ECommerce.Application.Auth.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    // Burada zorunlu giriş bilgilerini doğrularken cihaz adını isteğe bağlı ve yalnız uzunlukla sınırlı tutuyorum.
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(command => command.IpAddress)
            .MaximumLength(80);

        RuleFor(command => command.DeviceName)
            .MaximumLength(200)
            .When(command => command.DeviceName is not null);
    }
}
