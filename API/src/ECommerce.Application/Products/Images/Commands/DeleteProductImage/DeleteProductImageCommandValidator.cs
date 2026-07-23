using FluentValidation;

namespace ECommerce.Application.Products.Images.Commands.DeleteProductImage;

public sealed class DeleteProductImageCommandValidator : AbstractValidator<DeleteProductImageCommand>
{
    public DeleteProductImageCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}
