using ECommerce.Application.Orders.Commands.ReconcileOrderCancellations;
using MediatR;

namespace ECommerce.API.BackgroundServices;

public sealed class OrderCancellationReconciliationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderCancellationReconciliationBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);
    private const int BatchSize = 25;

    // Burada cancellation worker'ın scope factory ve güvenli logger bağımlılıklarını hazırlıyorum.
    public OrderCancellationReconciliationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<OrderCancellationReconciliationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // Burada reconciliation batch'lerini bounded aralıkla çalıştırıp provider kimliği veya PII loglamıyorum.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var result = await sender.Send(
                    new ReconcileOrderCancellationsCommand(BatchSize),
                    stoppingToken);
                if (result.ClaimedCount > 0)
                {
                    _logger.LogInformation(
                        "Order cancellation reconciliation batch processed. Claimed={ClaimedCount}, Completed={CompletedCount}",
                        result.ClaimedCount,
                        result.CompletedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Order cancellation reconciliation batch failed.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
            {
                break;
            }
        }
    }
}
