using FluentValidation;

namespace ECommerce.Application.ProductTypes.Commands.DeleteProductType;

public sealed class DeleteProductTypeCommandValidator : AbstractValidator<DeleteProductTypeCommand>
{
    // Burada silinecek ürün türü kimliğinin boş olmamasını doğruluyorum.
    public DeleteProductTypeCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
