using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.API.BackgroundServices;

public sealed class EmailOutboxBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailOutboxBackgroundService> _logger;
    private readonly string _workerId;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _leaseDuration;
    private readonly TimeSpan _sendTimeout;
    private readonly int _batchSize;

    // Burada e-posta worker'ını scoped servisleri güvenli biçimde çözecek ve tekil kimlik taşıyacak şekilde hazırlıyorum.
    public EmailOutboxBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<EmailOutboxBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _workerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";
        _interval = TimeSpan.FromSeconds(GetBoundedPositiveInt(
            configuration["Email:Outbox:PollIntervalSeconds"],
            15,
            5,
            300));
        _leaseDuration = TimeSpan.FromSeconds(GetBoundedPositiveInt(
            configuration["Email:Outbox:LeaseSeconds"],
            600,
            60,
            1800));
        var maximumSendTimeoutSeconds = Math.Max(5, (int)_leaseDuration.TotalSeconds - 5);
        _sendTimeout = TimeSpan.FromSeconds(GetBoundedPositiveInt(
            configuration["Email:Outbox:SendTimeoutSeconds"],
            Math.Min(120, maximumSendTimeoutSeconds),
            5,
            maximumSendTimeoutSeconds));
        _batchSize = GetBoundedPositiveInt(configuration["Email:Outbox:BatchSize"], 20, 1, 100);
    }

    // Burada e-posta kuyruğunu uygulama açılışında ve düzenli aralıklarla işliyorum.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcessBatchAsync(stoppingToken);
        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessBatchAsync(stoppingToken);
        }
    }

    // Burada e-postaları önce veritabanında claim edip ardından yalnızca sahip olunan mesajları SMTP'ye iletiyorum.
    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
            var repository = scope.ServiceProvider.GetRequiredService<IEmailOutboxRepository>();
            var protector = scope.ServiceProvider.GetRequiredService<IPasswordResetTokenProtector>();
            var guestProtector = scope.ServiceProvider.GetRequiredService<IGuestOrderAccessTokenProtector>();
            var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
            var utcNow = clock.UtcNow;
            var expiredMessageCount = await repository.ExpirePendingAsync(
                utcNow,
                _batchSize,
                cancellationToken);
            if (expiredMessageCount > 0)
            {
                _logger.LogInformation(
                    "Süresi geçen {ExpiredMessageCount} e-posta outbox mesajı terminal dead-letter durumuna alındı.",
                    expiredMessageCount);
            }

            var messages = await repository.ClaimPendingAsync(
                _workerId,
                utcNow,
                utcNow.Add(_leaseDuration),
                _batchSize,
                cancellationToken);

            foreach (var message in messages)
            {
                if (message.ClaimToken is not { } claimToken)
                {
                    _logger.LogError(
                        "Claim token olmadan e-posta gönderimi atlandı. OutboxId: {OutboxId}",
                        message.Id);
                    continue;
                }

                try
                {
                    var beforeSendUtcNow = clock.UtcNow;
                    if (message.IsExpired(beforeSendUtcNow))
                    {
                        var expired = await repository.ExpireClaimAsync(
                            message.Id,
                            claimToken,
                            _workerId,
                            beforeSendUtcNow,
                            cancellationToken);
                        if (!expired)
                        {
                            _logger.LogWarning(
                                "Süresi geçen e-posta mesajı claim sahibi tarafından terminal duruma alınamadı. OutboxId: {OutboxId}, Type: {EmailType}",
                                message.Id,
                                message.Type);
                        }

                        continue;
                    }

                    var renewed = await repository.RenewClaimAsync(
                        message.Id,
                        claimToken,
                        _workerId,
                        beforeSendUtcNow,
                        beforeSendUtcNow.Add(_leaseDuration),
                        cancellationToken);
                    if (!renewed)
                    {
                        _logger.LogWarning(
                            "SMTP öncesi e-posta claim lease'i yenilenemedi. OutboxId: {OutboxId}, Type: {EmailType}",
                            message.Id,
                            message.Type);
                        continue;
                    }

                    using var sendTimeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    sendTimeoutSource.CancelAfter(GetSendTimeout(message, beforeSendUtcNow));
                    await SendMessageAsync(message, protector, guestProtector, sender, sendTimeoutSource.Token);
                    var afterSendUtcNow = clock.UtcNow;
                    if (message.IsExpired(afterSendUtcNow))
                    {
                        var expired = await repository.ExpireClaimAsync(
                            message.Id,
                            claimToken,
                            _workerId,
                            afterSendUtcNow,
                            cancellationToken);
                        if (!expired)
                        {
                            _logger.LogWarning(
                                "SMTP sonrası süresi geçen e-posta mesajı terminal duruma alınamadı. OutboxId: {OutboxId}, Type: {EmailType}",
                                message.Id,
                                message.Type);
                        }

                        continue;
                    }

                    var completed = await repository.CompleteClaimAsync(
                        message.Id,
                        claimToken,
                        _workerId,
                        afterSendUtcNow,
                        cancellationToken);

                    if (!completed)
                    {
                        _logger.LogWarning(
                            "E-posta SMTP sonrası tamamlanamadı çünkü lease artık worker'a ait değil. OutboxId: {OutboxId}, Type: {EmailType}",
                            message.Id,
                            message.Type);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    var failed = await repository.FailClaimAsync(
                        message.Id,
                        claimToken,
                        _workerId,
                        clock.UtcNow,
                        exception.Message,
                        cancellationToken);

                    if (failed)
                    {
                        if (message.DeadLetteredAt.HasValue)
                        {
                            _logger.LogError(
                                exception,
                                "E-posta en fazla {MaximumDeliveryAttempts} denemeden sonra dead-letter durumuna alındı. OutboxId: {OutboxId}, Type: {EmailType}",
                                EmailOutboxMessage.MaximumDeliveryAttempts,
                                message.Id,
                                message.Type);
                        }
                        else
                        {
                            _logger.LogWarning(
                                exception,
                                "E-posta gönderilemedi ve yeniden deneme planlandı. OutboxId: {OutboxId}, Type: {EmailType}",
                                message.Id,
                                message.Type);
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            exception,
                            "E-posta gönderim hatası kaydedilemedi çünkü lease artık worker'a ait değil. OutboxId: {OutboxId}, Type: {EmailType}",
                            message.Id,
                            message.Type);
                    }
                }
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
        IGuestOrderAccessTokenProtector guestProtector,
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
            EmailOutboxMessageType.OrderCreated => sender.SendOrderCreatedAsync(
                message.Email,
                message.RecipientName
                    ?? throw new InvalidOperationException("Order recipient name is missing."),
                message.OrderNumber
                    ?? throw new InvalidOperationException("Order number is missing."),
                message.Amount
                    ?? throw new InvalidOperationException("Order amount is missing."),
                cancellationToken),
            EmailOutboxMessageType.PaymentPaid => sender.SendPaymentPaidAsync(
                message.Email,
                message.RecipientName
                    ?? throw new InvalidOperationException("Payment recipient name is missing."),
                message.OrderNumber
                    ?? throw new InvalidOperationException("Order number is missing."),
                message.Amount
                    ?? throw new InvalidOperationException("Payment amount is missing."),
                cancellationToken),
            EmailOutboxMessageType.PaymentFailed => sender.SendPaymentFailedAsync(
                message.Email,
                message.RecipientName
                    ?? throw new InvalidOperationException("Payment recipient name is missing."),
                message.OrderNumber
                    ?? throw new InvalidOperationException("Order number is missing."),
                message.Amount
                    ?? throw new InvalidOperationException("Payment amount is missing."),
                cancellationToken),
            EmailOutboxMessageType.OrderStatusChanged => sender.SendOrderStatusChangedAsync(
                message.Email,
                message.RecipientName
                    ?? throw new InvalidOperationException("Order recipient name is missing."),
                message.OrderNumber
                    ?? throw new InvalidOperationException("Order number is missing."),
                message.Status
                    ?? throw new InvalidOperationException("Order status is missing."),
                cancellationToken),
            EmailOutboxMessageType.ReturnRequested => sender.SendReturnRequestedAsync(
                message.Email,
                message.RecipientName
                    ?? throw new InvalidOperationException("Return recipient name is missing."),
                message.OrderNumber
                    ?? throw new InvalidOperationException("Order number is missing."),
                message.ReturnNumber
                    ?? throw new InvalidOperationException("Return number is missing."),
                cancellationToken),
            EmailOutboxMessageType.ReturnStatusChanged => sender.SendReturnStatusChangedAsync(
                message.Email,
                message.RecipientName
                    ?? throw new InvalidOperationException("Return recipient name is missing."),
                message.OrderNumber
                    ?? throw new InvalidOperationException("Order number is missing."),
                message.ReturnNumber
                    ?? throw new InvalidOperationException("Return number is missing."),
                message.Status
                    ?? throw new InvalidOperationException("Return status is missing."),
                cancellationToken),
            EmailOutboxMessageType.GuestOrderAccess => sender.SendGuestOrderAccessAsync(
                message.Email,
                message.RecipientName
                    ?? throw new InvalidOperationException("Guest order recipient name is missing."),
                message.OrderNumber
                    ?? throw new InvalidOperationException("Guest order number is missing."),
                guestProtector.Unprotect(message.ProtectedToken
                    ?? throw new InvalidOperationException("Guest order access token is missing.")),
                message.ExpiresAt
                    ?? throw new InvalidOperationException("Guest order access expiry is missing."),
                cancellationToken),
            _ => throw new InvalidOperationException($"Email outbox type '{message.Type}' is not supported.")
        };
    }

    // Burada kısa ömürlü mesajlarda SMTP çağrısının token geçerlilik süresini aşmamasını sağlıyorum.
    private TimeSpan GetSendTimeout(EmailOutboxMessage message, DateTime utcNow)
    {
        if (!message.ExpiresAt.HasValue)
        {
            return _sendTimeout;
        }

        var remainingLifetime = message.ExpiresAt.Value - utcNow;
        return remainingLifetime < _sendTimeout
            ? remainingLifetime
            : _sendTimeout;
    }

    // Burada worker ayarını güvenli varsayılan ve kabul edilen sınırlar içinde tamsayı olarak okuyorum.
    private static int GetBoundedPositiveInt(string? value, int fallback, int minimum, int maximum)
    {
        return int.TryParse(value, out var parsedValue) &&
               parsedValue >= minimum &&
               parsedValue <= maximum
            ? parsedValue
            : fallback;
    }
}
