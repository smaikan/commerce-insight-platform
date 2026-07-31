using ECommerce.Domain.Entities;
using FluentValidation;

namespace ECommerce.Application.ShippingMethods.Commands.CreateShippingMethod;

public sealed class CreateShippingMethodCommandValidator : AbstractValidator<CreateShippingMethodCommand>
{
    // Burada yeni kargo yöntemi isteğinin ad, ücret ve sıralama sınırlarını doğruluyorum.
    public CreateShippingMethodCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(ShippingMethod.MaximumNameLength);

        RuleFor(command => command.FixedFee)
            .GreaterThanOrEqualTo(0m);

        RuleFor(command => command.DisplayOrder)
            .GreaterThanOrEqualTo(0);
    }
}
