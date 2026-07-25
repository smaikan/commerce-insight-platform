using ECommerce.API.Configuration;
using ECommerce.Application.Orders.Commands.ExpireStockReservations;
using MediatR;
using Microsoft.Extensions.Options;

namespace ECommerce.API.BackgroundServices;

// Burada süresi dolmuş, güvenle iptal edilebilen stok rezervasyonlarını düzenli aralıklarla işleyen worker'ı tanımlıyorum.
public sealed class OrderReservationExpirationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderReservationExpirationBackgroundService> _logger;
    private readonly TimeSpan _interval;
    private readonly int _batchSize;

    // Burada worker'ın scope, log ve doğrulanmış rezervasyon çalışma ayarlarını hazırlıyorum.
    public OrderReservationExpirationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<OrderReservationExpirationBackgroundService> logger,
        IOptions<OrderReservationOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var value = options.Value;
        if (value.SweepIntervalSeconds is < 5 or > 3_600)
        {
            throw new InvalidOperationException("Order reservation sweep interval must be between 5 seconds and 1 hour.");
        }

        if (value.BatchSize is < 1 or > 500)
        {
            throw new InvalidOperationException("Order reservation batch size must be between 1 and 500.");
        }

        _interval = TimeSpan.FromSeconds(value.SweepIntervalSeconds);
        _batchSize = value.BatchSize;
    }

    // Burada uygulama başlarken bir parti çalıştırıp ardından düzenli zamanlayıcıyla devam ediyorum.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ProcessExpiredReservationsAsync(stoppingToken);
        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessExpiredReservationsAsync(stoppingToken);
        }
    }

    // Burada scoped MediatR akışıyla iptal edilen ve belirsiz ödeme nedeniyle atlanan rezervasyonları gözlemlüyorum.
    private async Task ProcessExpiredReservationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var result = await sender.Send(
                new ExpireStockReservationsCommand(_batchSize),
                cancellationToken);
            if (result.CancelledOrderCount > 0 || result.SkippedPendingPaymentCount > 0)
            {
                _logger.LogInformation(
                    "Stok rezervasyon partisi tamamlandı. Iptal edilen: {CancelledOrderCount}, belirsiz ödeme nedeniyle atlanan: {SkippedPendingPaymentCount}",
                    result.CancelledOrderCount,
                    result.SkippedPendingPaymentCount);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Stok rezervasyonu sonlandırma worker'ı başarısız oldu.");
        }
    }
}
