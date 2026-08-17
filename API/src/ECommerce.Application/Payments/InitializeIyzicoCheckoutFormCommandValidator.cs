using ECommerce.Domain.Entities;
using FluentValidation;

namespace ECommerce.Application.Payments;

public sealed class InitializeIyzicoCheckoutFormCommandValidator
    : AbstractValidator<InitializeIyzicoCheckoutFormCommand>
{
    // Burada hosted ödeme formunun sipariş, retry anahtarı ve istemci IP girdilerini doğruluyorum.
    public InitializeIyzicoCheckoutFormCommandValidator()
    {
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.IdempotencyKey)
            .NotEmpty()
            .MinimumLength(16)
            .MaximumLength(Payment.MaximumIdempotencyKeyLength)
            .Matches(Payment.IdempotencyKeyPattern);
        RuleFor(command => command.ClientIpAddress).NotEmpty().MaximumLength(64);
    }
}
