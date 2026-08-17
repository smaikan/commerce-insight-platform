using FluentValidation;

namespace ECommerce.Application.ProductTypes.Commands.UpdateProductType;

public sealed class UpdateProductTypeCommandValidator : AbstractValidator<UpdateProductTypeCommand>
{
    // Burada ürün türü güncelleme alanlarının sınırlarını tanımlıyorum.
    public UpdateProductTypeCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(command => command.Description)
            .MaximumLength(1000);

        RuleFor(command => command.ImageUrl)
            .MaximumLength(ECommerce.Domain.Entities.ProductType.MaximumImageUrlLength);
    }
}
