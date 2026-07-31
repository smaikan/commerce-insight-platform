using ECommerce.Domain.Entities;
using FluentValidation;

namespace ECommerce.Application.Orders.Commands.CreatePayment;

public sealed class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    // Burada ödeme isteğinin kimlik, sağlayıcı ve tekrar güvenliği anahtarını doğruluyorum.
    public CreatePaymentCommandValidator()
    {
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.Provider).IsInEnum();
        RuleFor(command => command.IdempotencyKey)
            .NotEmpty()
            .MinimumLength(16)
            .MaximumLength(80)
            .Matches(Payment.IdempotencyKeyPattern);
    }
}
