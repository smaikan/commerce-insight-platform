using ECommerce.Domain.Entities;
using FluentValidation;

namespace ECommerce.Application.Carts.Commands.AddCartItem;

public sealed class AddCartItemCommandValidator : AbstractValidator<AddCartItemCommand>
{
    // Burada sepete ekleme isteğinin varyant, adet, session ve concurrency değerlerini doğruluyorum.
    public AddCartItemCommandValidator()
    {
        RuleFor(command => command.ProductVariantId).NotEmpty();
        RuleFor(command => command.Quantity).GreaterThan(0);
        RuleFor(command => command.SessionId)
            .MaximumLength(Cart.MaximumSessionIdLength);
        RuleFor(command => command.ExpectedConcurrencyToken)
            .Must(token => !token.HasValue || token.Value != Guid.Empty)
            .WithMessage("Expected concurrency token cannot be empty.");
    }
}
