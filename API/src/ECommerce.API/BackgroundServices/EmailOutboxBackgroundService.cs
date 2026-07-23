using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.API.BackgroundServices;

public sealed class EmailOutboxBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailOutboxBackgroundService> _logger;

    // Burada e-posta worker'ını scoped servisleri güvenli biçimde çözebilecek şekilde hazırlıyorum.
    public EmailOutboxBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<EmailOutboxBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // Burada e-posta kuyruğunu uygulama açılışında ve düzenli aralıklarla işliyorum.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcessBatchAsync(stoppingToken);
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessBatchAsync(stoppingToken);
        }
    }

    // Burada gönderim zamanı gelen e-postaları küçük bir batch halinde SMTP'ye iletiyorum.
    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
            var repository = scope.ServiceProvider.GetRequiredService<IEmailOutboxRepository>();
            var protector = scope.ServiceProvider.GetRequiredService<IPasswordResetTokenProtector>();
            var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var messages = await repository.GetPendingForUpdateAsync(clock.UtcNow, 20, cancellationToken);

            foreach (var message in messages)
            {
                try
                {
                    await SendMessageAsync(message, protector, sender, cancellationToken);
                    message.MarkProcessed(clock.UtcNow);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    message.RecordFailure(clock.UtcNow, exception.Message);
                    _logger.LogWarning(
                        exception,
                        "E-posta gönderilemedi. OutboxId: {OutboxId}, Type: {EmailType}",
                        message.Id,
                        message.Type);
                }
            }

            if (messages.Count > 0)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "E-posta outbox işlemi başarısız oldu.");
        }
    }

    // Burada outbox mesaj türüne uygun güvenilir e-posta template'ini gönderiyorum.
    private static Task SendMessageAsync(
        EmailOutboxMessage message,
        IPasswordResetTokenProtector protector,
        IEmailSender sender,
        CancellationToken cancellationToken)
    {
        return message.Type switch
        {
            EmailOutboxMessageType.PasswordReset => sender.SendPasswordResetAsync(
                message.Email,
                protector.Unprotect(message.ProtectedToken
                    ?? throw new InvalidOperationException("Password reset token is missing.")),
                message.ExpiresAt
                    ?? throw new InvalidOperationException("Password reset expiry is missing."),
                cancellationToken),
            EmailOutboxMessageType.Welcome => sender.SendWelcomeAsync(
                message.Email,
                message.RecipientName
                    ?? throw new InvalidOperationException("Welcome email recipient name is missing."),
                cancellationToken),
            _ => throw new InvalidOperationException($"Email outbox type '{message.Type}' is not supported.")
        };
    }
}
