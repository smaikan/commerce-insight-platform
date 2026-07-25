using FluentValidation;

namespace ECommerce.Application.Orders.Commands.ChangeOrderStatus;

public sealed class ChangeOrderStatusCommandValidator : AbstractValidator<ChangeOrderStatusCommand>
{
    // Burada yönetim durum değişikliği isteğinin kimlik ve enum değerini doğruluyorum.
    public ChangeOrderStatusCommandValidator()
    {
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.Status).IsInEnum();
        RuleFor(command => command.Status)
            .Must(status => status is not ECommerce.Domain.Enums.OrderStatus.Refunded and
                not ECommerce.Domain.Enums.OrderStatus.ReturnRequested and
                not ECommerce.Domain.Enums.OrderStatus.ReturnApproved)
            .WithMessage(
                "Refunded and return statuses cannot be set through the order status endpoint. " +
                "Use the dedicated return workflow or a provider-confirmed refund integration.");
    }
}
