using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;

namespace ECommerce.API.BackgroundServices;

public sealed class UserTokenCleanupBackgroundService : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(12);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserTokenCleanupBackgroundService> _logger;

    public UserTokenCleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<UserTokenCleanupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // Burada eski kullanıcı tokenlarını periyodik olarak veritabanından temizliyorum.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CleanupInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var cleanupService = scope.ServiceProvider.GetRequiredService<IUserTokenCleanupService>();
                var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
                var deletedCount = await cleanupService.CleanupAsync(
                    dateTimeProvider.UtcNow.AddDays(-30),
                    stoppingToken);
                _logger.LogInformation("{DeletedCount} eski kullanıcı tokenı temizlendi.", deletedCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Kullanıcı token temizliği sırasında hata oluştu.");
            }
        }
    }
}
