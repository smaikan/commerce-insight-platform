using FluentValidation;

namespace ECommerce.Application.Products.Engagement.Commands.RecordProductActivity;

public sealed class RecordProductActivityCommandValidator : AbstractValidator<RecordProductActivityCommand>
{
    // Burada müşteriden gelen ürün hareketinin güvenli ve desteklenen türde olduğunu doğruluyorum.
    public RecordProductActivityCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.ActivityType)
            .Equal(ProductActivityType.Click)
            .WithMessage("Customers can only record click activity directly.");
        RuleFor(command => command.Quantity)
            .Equal(1);
        RuleFor(command => command.ProductVariantId)
            .Empty()
            .WithMessage("Click activity cannot target a product variant.");
    }
}
