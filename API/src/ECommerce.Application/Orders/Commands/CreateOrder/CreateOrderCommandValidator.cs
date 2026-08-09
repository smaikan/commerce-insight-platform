using ECommerce.Domain.Entities;
using FluentValidation;

namespace ECommerce.Application.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    // Burada siparişe dönüştürülecek sepetin concurrency token zorunluluğunu doğruluyorum.
    public CreateOrderCommandValidator()
    {
        RuleFor(command => command.ExpectedCartConcurrencyToken)
            .NotEmpty();
        RuleFor(command => command.ShippingAddressId)
            .NotEmpty()
            .WithMessage("A shipping address is required.");
        RuleFor(command => command.ShippingMethodId)
            .NotEmpty()
            .WithMessage("An active shipping method is required.");
        RuleFor(command => command.CouponCode)
            .MaximumLength(Coupon.MaximumCodeLength)
            .Matches(Coupon.CodePattern)
            .When(command => !string.IsNullOrWhiteSpace(command.CouponCode));
    }
}
