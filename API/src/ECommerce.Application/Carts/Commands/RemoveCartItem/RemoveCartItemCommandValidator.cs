using ECommerce.Domain.Entities;
using FluentValidation;

namespace ECommerce.Application.Carts.Commands.RemoveCartItem;

public sealed class RemoveCartItemCommandValidator : AbstractValidator<RemoveCartItemCommand>
{
    // Burada satır silme isteğinin kimlik, token ve session alanlarını doğruluyorum.
    public RemoveCartItemCommandValidator()
    {
        RuleFor(command => command.CartItemId).NotEmpty();
        RuleFor(command => command.ExpectedConcurrencyToken).NotEmpty();
        RuleFor(command => command.SessionId)
            .MaximumLength(Cart.MaximumSessionIdLength);
    }
}
