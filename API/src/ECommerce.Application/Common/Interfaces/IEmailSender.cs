namespace ECommerce.Application.Common.Interfaces;

public interface IEmailSender
{
    // Burada parola sıfırlama e-postasının gönderim sözleşmesini tanımlıyorum.
    Task SendPasswordResetAsync(
        string email,
        string rawToken,
        DateTime expiresAt,
        CancellationToken cancellationToken = default);

    // Burada kayıt sonrası hoş geldin e-postasının gönderim sözleşmesini tanımlıyorum.
    Task SendWelcomeAsync(
        string email,
        string recipientName,
        CancellationToken cancellationToken = default);

    // Burada siparişin başarıyla oluşturulduğunu bildiren e-postanın gönderim sözleşmesini tanımlıyorum.
    Task SendOrderCreatedAsync(
        string email,
        string recipientName,
        string orderNumber,
        decimal grandTotal,
        CancellationToken cancellationToken = default);

    // Burada başarılı ödeme bildirim e-postasının gönderim sözleşmesini tanımlıyorum.
    Task SendPaymentPaidAsync(
        string email,
        string recipientName,
        string orderNumber,
        decimal amount,
        CancellationToken cancellationToken = default);

    // Burada başarısız ödeme bildirim e-postasının gönderim sözleşmesini tanımlıyorum.
    Task SendPaymentFailedAsync(
        string email,
        string recipientName,
        string orderNumber,
        decimal amount,
        CancellationToken cancellationToken = default);

    // Burada sipariş durum ve opsiyonel kargo takip e-postasının gönderim sözleşmesini tanımlıyorum.
    Task SendOrderStatusChangedAsync(
        string email,
        string recipientName,
        string orderNumber,
        string status,
        string? shippingCarrier = null,
        string? trackingNumber = null,
        string? trackingUrl = null,
        CancellationToken cancellationToken = default);

    // Burada iade talebinin açıldığını bildiren e-postanın gönderim sözleşmesini tanımlıyorum.
    Task SendReturnRequestedAsync(
        string email,
        string recipientName,
        string orderNumber,
        string returnNumber,
        CancellationToken cancellationToken = default);

    // Burada iade durum değişikliği e-postasının gönderim sözleşmesini tanımlıyorum.
    Task SendReturnStatusChangedAsync(
        string email,
        string recipientName,
        string orderNumber,
        string returnNumber,
        string status,
        CancellationToken cancellationToken = default);

    // Burada guest sipariş magic-link e-postasının gönderim sözleşmesini tanımlıyorum.
    Task SendGuestOrderAccessAsync(
        string email,
        string recipientName,
        string orderNumber,
        string rawToken,
        DateTime expiresAt,
        CancellationToken cancellationToken = default);

    // Burada operasyonel inbox'a güvenli iletişim başvurusu bildirimini gönderme sözleşmesini tanımlıyorum.
    Task SendContactMessageReceivedAsync(
        string inboxEmail,
        string referenceNumber,
        string name,
        string customerEmail,
        string? phone,
        string subject,
        string? providedOrderNumber,
        string body,
        string? adminDetailUrl,
        CancellationToken cancellationToken = default);

    // Burada kayıtlı alıcıya güvenli destek Reply-To ile contact reply gönderme sözleşmesini tanımlıyorum.
    Task SendContactMessageReplyAsync(
        string recipientEmail,
        string recipientName,
        string referenceNumber,
        string body,
        CancellationToken cancellationToken = default);
}
