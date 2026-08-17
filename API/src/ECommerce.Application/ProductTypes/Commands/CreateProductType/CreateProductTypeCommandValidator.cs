using FluentValidation;

namespace ECommerce.Application.ProductTypes.Commands.CreateProductType;

public sealed class CreateProductTypeCommandValidator : AbstractValidator<CreateProductTypeCommand>
{
    // Burada ürün türü oluşturma alanlarının sınırlarını tanımlıyorum.
    public CreateProductTypeCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(command => command.Description)
            .MaximumLength(1000);

        RuleFor(command => command.ImageUrl)
            .MaximumLength(ECommerce.Domain.Entities.ProductType.MaximumImageUrlLength);
    }
}
