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
}
