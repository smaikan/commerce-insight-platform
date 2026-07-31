using ECommerce.Domain.Entities;
using FluentValidation;

namespace ECommerce.Application.Carts.Commands.ClearCart;

public sealed class ClearCartCommandValidator : AbstractValidator<ClearCartCommand>
{
    // Burada sepet temizleme isteğinin concurrency tokenı ve session alanını doğruluyorum.
    public ClearCartCommandValidator()
    {
        RuleFor(command => command.ExpectedConcurrencyToken).NotEmpty();
        RuleFor(command => command.SessionId)
            .MaximumLength(Cart.MaximumSessionIdLength);
    }
}
