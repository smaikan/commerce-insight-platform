using ECommerce.Domain.Entities;
using FluentValidation;

namespace ECommerce.Application.ShippingMethods.Commands.UpdateShippingMethod;

public sealed class UpdateShippingMethodCommandValidator : AbstractValidator<UpdateShippingMethodCommand>
{
    // Burada kargo yöntemi güncellemesinin kimlik, ad, ücret ve sıralama sınırlarını doğruluyorum.
    public UpdateShippingMethodCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(ShippingMethod.MaximumNameLength);

        RuleFor(command => command.FixedFee)
            .GreaterThanOrEqualTo(0m);

        RuleFor(command => command.DisplayOrder)
            .GreaterThanOrEqualTo(0);
    }
}
