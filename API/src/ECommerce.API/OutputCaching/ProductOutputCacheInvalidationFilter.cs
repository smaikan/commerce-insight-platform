using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.OutputCaching;

// Burada katalog sınıflandırma mutasyonlarından sonra public ürün ve facet cache'ini birlikte temizliyorum.
public sealed class ProductOutputCacheInvalidationFilter : IAsyncActionFilter
{
    private readonly IOutputCacheStore _outputCacheStore;
    private readonly ILogger<ProductOutputCacheInvalidationFilter> _logger;

    // Burada ürün cache etiketini temizleyecek output cache deposunu hazırlıyorum.
    public ProductOutputCacheInvalidationFilter(
        IOutputCacheStore outputCacheStore,
        ILogger<ProductOutputCacheInvalidationFilter> logger)
    {
        _outputCacheStore = outputCacheStore;
        _logger = logger;
    }

    // Burada başarılı GET dışı sınıflandırma işlemlerinden sonra ortak ürün cache etiketini geçersiz kılıyorum.
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executedContext = await next();
        if (HttpMethods.IsGet(context.HttpContext.Request.Method) ||
            HttpMethods.IsHead(context.HttpContext.Request.Method) ||
            executedContext.Exception is not null)
        {
            return;
        }

        var statusCode = (executedContext.Result as IStatusCodeActionResult)?.StatusCode
            ?? StatusCodes.Status200OK;
        if (statusCode < StatusCodes.Status400BadRequest)
        {
            try
            {
                await _outputCacheStore.EvictByTagAsync("products", CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ürün output cache temizliği başarısız oldu ancak ana işlem korundu.");
            }
        }
    }
}
