using MediatR;

namespace ECommerce.Application.Payments;

public sealed record InitializeIyzicoCheckoutFormCommand(
    Guid OrderId,
    string IdempotencyKey,
    string ClientIpAddress) : IRequest<CheckoutFormSessionDto>;

public sealed class InitializeIyzicoCheckoutFormCommandHandler
    : IRequestHandler<InitializeIyzicoCheckoutFormCommand, CheckoutFormSessionDto>
{
    private readonly CheckoutFormPaymentService _payments;

    // Burada üye CheckoutForm komutunu ortak ödeme uygulama servisine bağlarım.
    public InitializeIyzicoCheckoutFormCommandHandler(CheckoutFormPaymentService payments)
    {
        _payments = payments;
    }

    // Burada kimliği doğrulanmış kullanıcının hosted ödeme formunu başlatıyorum.
    public Task<CheckoutFormSessionDto> Handle(
        InitializeIyzicoCheckoutFormCommand request,
        CancellationToken cancellationToken)
    {
        return _payments.InitializeForCurrentUserAsync(
            request.OrderId,
            request.IdempotencyKey,
            request.ClientIpAddress,
            cancellationToken);
    }
}
