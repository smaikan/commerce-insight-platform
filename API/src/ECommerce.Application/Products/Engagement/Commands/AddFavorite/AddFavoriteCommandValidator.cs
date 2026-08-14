using FluentValidation;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Products.Engagement.Commands.AddFavorite;

public sealed class AddFavoriteCommandValidator : AbstractValidator<AddFavoriteCommand>
{
    // Burada ürün kimliği ile varsa guest session uzunluğunu doğruluyorum.
    public AddFavoriteCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.SessionId)
            .MaximumLength(FavoriteProduct.MaximumSessionIdLength);
    }
}
