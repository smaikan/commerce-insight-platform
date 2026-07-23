using FluentValidation;

namespace ECommerce.Application.Products.Engagement.Commands.AddFavorite;

public sealed class AddFavoriteCommandValidator : AbstractValidator<AddFavoriteCommand>
{
    public AddFavoriteCommandValidator() => RuleFor(command => command.ProductId).NotEmpty();
}
