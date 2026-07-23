using FluentValidation;

namespace ECommerce.Application.Products.Engagement.Commands.RecordProductActivity;

public sealed class RecordProductActivityCommandValidator : AbstractValidator<RecordProductActivityCommand>
{
    // Burada müşteriden gelen ürün hareketinin güvenli ve desteklenen türde olduğunu doğruluyorum.
    public RecordProductActivityCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.ActivityType)
            .Must(activityType => activityType is ProductActivityType.Click or ProductActivityType.AddToCart)
            .WithMessage("Customers can only record click and add-to-cart activities.");
        RuleFor(command => command.Quantity)
            .Equal(1)
            .When(command => command.ActivityType == ProductActivityType.Click);
        RuleFor(command => command.Quantity)
            .InclusiveBetween(1, 100)
            .When(command => command.ActivityType == ProductActivityType.AddToCart);
        RuleFor(command => command.ProductVariantId)
            .NotEmpty()
            .When(command => command.ActivityType == ProductActivityType.AddToCart);
    }
}
