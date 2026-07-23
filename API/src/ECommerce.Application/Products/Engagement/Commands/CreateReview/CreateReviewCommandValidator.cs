using FluentValidation;

namespace ECommerce.Application.Products.Engagement.Commands.CreateReview;

public sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    // Burada ürün yorumu isteğinin alanlarını doğruluyorum.
    public CreateReviewCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.Comment).NotEmpty().MaximumLength(4000);
        RuleFor(command => command.Title).MaximumLength(250);
        RuleFor(command => command.RatingValue).InclusiveBetween(1, 5).When(command => command.RatingValue.HasValue);
    }
}
