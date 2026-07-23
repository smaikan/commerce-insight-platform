namespace ECommerce.Application.Common.Interfaces;

public interface IUserTokenCleanupService
{
    Task<int> CleanupAsync(DateTime retentionCutoff, CancellationToken cancellationToken = default);
}
