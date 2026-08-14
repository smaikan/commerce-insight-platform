using FluentValidation;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

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
        When(command => command.Status == OrderStatus.Shipped, () =>
        {
            RuleFor(command => command.ShippingCarrier)
                .NotEmpty()
                .MaximumLength(Order.MaximumShippingCarrierLength);
            RuleFor(command => command.TrackingNumber)
                .NotEmpty()
                .MaximumLength(Order.MaximumTrackingNumberLength);
            RuleFor(command => command.TrackingUrl)
                .MaximumLength(Order.MaximumTrackingUrlLength)
                .Must(BeOptionalHttpUrl)
                .WithMessage("TrackingUrl must be an absolute HTTP or HTTPS URL.");
        });
    }

    // Burada opsiyonel takip bağlantısının yalnız mutlak HTTP veya HTTPS adresi olmasını doğruluyorum.
    private static bool BeOptionalHttpUrl(string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        (Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) &&
         (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));
}
