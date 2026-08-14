using FluentValidation;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Products.Engagement.Commands.RemoveFavorite;

public sealed class RemoveFavoriteCommandValidator : AbstractValidator<RemoveFavoriteCommand>
{
    // Burada ürün kimliği ile varsa guest session uzunluğunu doğruluyorum.
    public RemoveFavoriteCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.SessionId)
            .MaximumLength(FavoriteProduct.MaximumSessionIdLength);
    }
}
