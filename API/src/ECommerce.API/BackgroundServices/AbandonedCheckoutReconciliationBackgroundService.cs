using ECommerce.API.Configuration;
using ECommerce.Application.Payments;
using MediatR;
using Microsoft.Extensions.Options;

namespace ECommerce.API.BackgroundServices;

// Burada müşterinin terk ettiği iyzico oturumlarını ve olası geç tahsilatları düzenli işleyen worker'ı tanımlıyorum.
public sealed class AbandonedCheckoutReconciliationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AbandonedCheckoutReconciliationBackgroundService> _logger;
    private readonly TimeSpan _interval;
    private readonly int _batchSize;

    // Burada worker'ın scope, log ve mevcut rezervasyon tarama sınırlarını hazırlıyorum.
    public AbandonedCheckoutReconciliationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AbandonedCheckoutReconciliationBackgroundService> logger,
        IOptions<OrderReservationOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(options.Value.SweepIntervalSeconds);
        _batchSize = options.Value.BatchSize;
    }

    // Burada uygulama başlangıcında ve her periyotta terk edilmiş ödeme uzlaştırmasını çalıştırıyorum.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcessAsync(stoppingToken);
        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessAsync(stoppingToken);
        }
    }

    // Burada bir worker turunu ayrı scope'ta çalıştırıp yalnız operasyon sayılarını güvenli logluyorum.
    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var result = await sender.Send(
                new ReconcileAbandonedCheckoutFormsCommand(_batchSize),
                cancellationToken);
            if (result.CandidateCount > 0)
            {
                _logger.LogInformation(
                    "Terk edilmiş ödeme uzlaştırması tamamlandı. Aday: {CandidateCount}, terminal: {CompletedCount}",
                    result.CandidateCount,
                    result.CompletedCount);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Terk edilmiş ödeme uzlaştırma worker'ı başarısız oldu.");
        }
    }
}
