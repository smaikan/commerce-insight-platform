using FluentValidation;

namespace ECommerce.Application.Products.Engagement.Commands.RemoveFavorite;

public sealed class RemoveFavoriteCommandValidator : AbstractValidator<RemoveFavoriteCommand>
{
    public RemoveFavoriteCommandValidator() => RuleFor(command => command.ProductId).NotEmpty();
}
