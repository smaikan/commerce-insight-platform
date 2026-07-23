using FluentValidation;

namespace ECommerce.Application.Products.Engagement.Commands.UpsertRating;

public sealed class UpsertRatingCommandValidator : AbstractValidator<UpsertRatingCommand>
{
    // Burada ürün puanlama isteğinin geçerli aralıkta olduğunu doğruluyorum.
    public UpsertRatingCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.RatingValue).InclusiveBetween(1, 5);
    }
}
