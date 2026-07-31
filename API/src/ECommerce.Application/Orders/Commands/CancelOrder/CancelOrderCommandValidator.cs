using FluentValidation;

namespace ECommerce.Application.Orders.Commands.CancelOrder;

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    // Burada iptal isteğinin geçerli sipariş kimliği taşıdığını doğruluyorum.
    public CancelOrderCommandValidator()
    {
        RuleFor(command => command.OrderId).NotEmpty();
    }
}
