using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Contacts;

namespace ECommerce.API.BackgroundServices;

public sealed class ContactRetentionBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ContactRetentionBackgroundService> _logger;
    private readonly int? _retentionDays;
    private readonly int _batchSize;

    // Burada contact retention worker bağımlılıklarını onaylı süre ve bounded batch ayarıyla hazırlıyorum.
    public ContactRetentionBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ContactRetentionBackgroundService> logger,
        ContactPrivacyOptions options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _retentionDays = options.RetentionDays;
        _batchSize = options.CleanupBatchSize;
    }

    // Burada retention anonimleştirmesini başlangıçta ve sonrasında günlük tek bounded batch olarak çalıştırıyorum.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await AnonymizeExpiredAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await AnonymizeExpiredAsync(stoppingToken);
        }
    }

    // Burada süresi dolan PII alanlarını serializable transaction içinde anonimleştirip audit metadata'sını koruyorum.
    private async Task AnonymizeExpiredAsync(CancellationToken cancellationToken)
    {
        if (!_retentionDays.HasValue)
        {
            return;
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IContactMessageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
            var utcNow = clock.UtcNow;
            var cutoffUtc = utcNow.AddDays(-_retentionDays.Value);
            var anonymized = await unitOfWork.ExecuteInSerializableTransactionAsync(async token =>
            {
                var prepared = await repository.PrepareExpiredForAnonymizationAsync(cutoffUtc, utcNow, _batchSize, token);
                if (prepared > 0)
                {
                    await unitOfWork.SaveChangesAsync(token);
                }

                return prepared;
            }, cancellationToken);
            if (anonymized > 0)
            {
                _logger.LogInformation("Retention süresi dolan {AnonymizedCount} contact mesajı anonimleştirildi.", anonymized);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Contact retention anonimleştirme işlemi başarısız oldu.");
        }
    }
}
