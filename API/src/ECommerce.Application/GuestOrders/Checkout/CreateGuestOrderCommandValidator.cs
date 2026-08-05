using ECommerce.Domain.Entities;
using FluentValidation;

namespace ECommerce.Application.GuestOrders.Checkout;

public sealed class CreateGuestOrderCommandValidator : AbstractValidator<CreateGuestOrderCommand>
{
    // Burada guest checkout'un PII, adres, kargo, concurrency ve idempotency alanlarını sınırlandırıyorum.
    public CreateGuestOrderCommandValidator()
    {
        RuleFor(command => command.CartSessionId).NotEmpty().MaximumLength(Cart.MaximumSessionIdLength);
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(200);
        RuleFor(command => command.ExpectedCartConcurrencyToken).NotEmpty();
        RuleFor(command => command.ShippingMethodId).NotEmpty();
        RuleFor(command => command.CouponCode).MaximumLength(Coupon.MaximumCodeLength);
        RuleFor(command => command.Customer.FirstName).NotEmpty().MaximumLength(OrderCustomerSnapshot.MaximumNameLength);
        RuleFor(command => command.Customer.LastName).NotEmpty().MaximumLength(OrderCustomerSnapshot.MaximumNameLength);
        RuleFor(command => command.Customer.Email).NotEmpty().EmailAddress().MaximumLength(OrderCustomerSnapshot.MaximumEmailLength);
        RuleFor(command => command.Customer.PhoneNumber).NotEmpty().MaximumLength(OrderCustomerSnapshot.MaximumPhoneNumberLength);
        RuleFor(command => command.ShippingAddress).NotNull().SetValidator(new CheckoutAddressInputValidator());
        RuleFor(command => command.BillingAddress!).SetValidator(new CheckoutAddressInputValidator())
            .When(command => command.BillingAddress is not null);
    }
}

public sealed class CheckoutAddressInputValidator : AbstractValidator<ECommerce.Application.Orders.Services.CheckoutAddressInput>
{
    // Burada guest adres snapshot alanlarının zorunluluk ve kalıcı depolama uzunluklarını doğruluyorum.
    public CheckoutAddressInputValidator()
    {
        RuleFor(address => address.Title).NotEmpty().MaximumLength(OrderAddressSnapshot.MaximumTitleLength);
        RuleFor(address => address.FirstName).NotEmpty().MaximumLength(OrderAddressSnapshot.MaximumNameLength);
        RuleFor(address => address.LastName).NotEmpty().MaximumLength(OrderAddressSnapshot.MaximumNameLength);
        RuleFor(address => address.PhoneNumber).NotEmpty().MaximumLength(OrderAddressSnapshot.MaximumPhoneNumberLength);
        RuleFor(address => address.City).NotEmpty().MaximumLength(OrderAddressSnapshot.MaximumCityLength);
        RuleFor(address => address.District).NotEmpty().MaximumLength(OrderAddressSnapshot.MaximumDistrictLength);
        RuleFor(address => address.FullAddress).NotEmpty().MaximumLength(OrderAddressSnapshot.MaximumFullAddressLength);
        RuleFor(address => address.PostalCode).MaximumLength(OrderAddressSnapshot.MaximumPostalCodeLength);
    }
}
