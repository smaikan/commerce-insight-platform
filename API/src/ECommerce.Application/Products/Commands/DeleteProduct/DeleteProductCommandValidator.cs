using FluentValidation;

namespace ECommerce.Application.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    // Burada silinecek ürünün geçerli bir iç kimliğe sahip olmasını doğruluyorum.
    public DeleteProductCommandValidator()
    {
        RuleFor(command => command.Id).GreaterThan(0);
    }
}
