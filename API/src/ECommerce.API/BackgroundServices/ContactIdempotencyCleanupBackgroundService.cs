using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Contacts;

namespace ECommerce.API.BackgroundServices;

public sealed class ContactIdempotencyCleanupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ContactIdempotencyCleanupBackgroundService> _logger;
    private readonly int _batchSize;

    // Burada bounded idempotency cleanup worker bağımlılıklarını ve batch sınırını hazırlıyorum.
    public ContactIdempotencyCleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ContactIdempotencyCleanupBackgroundService> logger,
        ContactPrivacyOptions options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _batchSize = options.CleanupBatchSize;
    }

    // Burada süresi dolmuş hash kayıtlarını günlük ve bounded batch'ler halinde temizliyorum.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CleanupAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CleanupAsync(stoppingToken);
        }
    }

    // Burada tek cleanup batch'ini scoped repository ve UTC saat ile çalıştırıyorum.
    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IContactMessageRepository>();
            var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
            var deleted = await repository.DeleteExpiredIdempotencyAsync(clock.UtcNow, _batchSize, cancellationToken);
            if (deleted > 0)
            {
                _logger.LogInformation("Süresi dolan {DeletedCount} contact idempotency kaydı temizlendi.", deleted);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Contact idempotency cleanup işlemi başarısız oldu.");
        }
    }
}
