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
            .When(command => command.ShippingAddressId.HasValue);
        RuleFor(command => command.ShippingMethodId)
            .NotEmpty()
            .When(command => command.ShippingMethodId.HasValue);
        RuleFor(command => command.ShippingAddressId)
            .NotEmpty()
            .When(command => command.ShippingMethodId.HasValue)
            .WithMessage("A shipping address is required when a shipping method is selected.");
        RuleFor(command => command.ShippingMethodId)
            .NotEmpty()
            .When(command => command.ShippingAddressId.HasValue)
            .WithMessage("A shipping method is required when a shipping address is selected.");
        RuleFor(command => command.CouponCode)
            .MaximumLength(Coupon.MaximumCodeLength)
            .Matches(Coupon.CodePattern)
            .When(command => !string.IsNullOrWhiteSpace(command.CouponCode));
    }
}
