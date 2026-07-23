using FluentValidation;

namespace ECommerce.Application.Products.Variants.Commands.DeleteProductVariant;

public sealed class DeleteProductVariantCommandValidator : AbstractValidator<DeleteProductVariantCommand>
{
    public DeleteProductVariantCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}
