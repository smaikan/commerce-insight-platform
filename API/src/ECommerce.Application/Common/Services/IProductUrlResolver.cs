namespace ECommerce.Application.Common.Services;

public interface IProductUrlResolver
{
    Task<string> ResolveAsync(
        string title,
        string? requestedUrl,
        long? excludedProductId = null,
        ISet<string>? requestReservedUrls = null,
        CancellationToken cancellationToken = default);
}
