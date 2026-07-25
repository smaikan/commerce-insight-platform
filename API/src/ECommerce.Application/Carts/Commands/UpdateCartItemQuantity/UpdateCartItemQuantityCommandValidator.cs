using ECommerce.Domain.Entities;
using FluentValidation;

namespace ECommerce.Application.Carts.Commands.UpdateCartItemQuantity;

public sealed class UpdateCartItemQuantityCommandValidator
    : AbstractValidator<UpdateCartItemQuantityCommand>
{
    // Burada sepet satırı güncellemesinin kimlik, adet, token ve session alanlarını doğruluyorum.
    public UpdateCartItemQuantityCommandValidator()
    {
        RuleFor(command => command.CartItemId).NotEmpty();
        RuleFor(command => command.Quantity).GreaterThan(0);
        RuleFor(command => command.ExpectedConcurrencyToken).NotEmpty();
        RuleFor(command => command.SessionId)
            .MaximumLength(Cart.MaximumSessionIdLength);
    }
}
