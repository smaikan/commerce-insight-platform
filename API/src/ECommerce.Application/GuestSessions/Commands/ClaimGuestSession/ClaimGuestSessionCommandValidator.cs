using ECommerce.Domain.Entities;
using FluentValidation;

namespace ECommerce.Application.GuestSessions.Commands.ClaimGuestSession;

public sealed class ClaimGuestSessionCommandValidator : AbstractValidator<ClaimGuestSessionCommand>
{
    // Burada claim edilecek session değerinin desteklenen biçim uzunluğunda olmasını doğruluyorum.
    public ClaimGuestSessionCommandValidator()
    {
        RuleFor(command => command.SessionId)
            .NotEmpty()
            .MaximumLength(Cart.MaximumSessionIdLength);
    }
}
