using ECommerce.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.OutputCaching;

// Burada ürün çıktısını etkileyen katalog mutasyonlarından sonra API output cache'i ve Storefront (Next.js) cache'ini anında temizliyorum.
public sealed class ProductOutputCacheInvalidationFilter : IAsyncActionFilter
{
    private readonly IOutputCacheStore _outputCacheStore;
    private readonly IStorefrontRevalidationService _revalidationService;
    private readonly ILogger<ProductOutputCacheInvalidationFilter> _logger;

    public ProductOutputCacheInvalidationFilter(
        IOutputCacheStore outputCacheStore,
        IStorefrontRevalidationService revalidationService,
        ILogger<ProductOutputCacheInvalidationFilter> logger)
    {
        _outputCacheStore = outputCacheStore;
        _revalidationService = revalidationService;
        _logger = logger;
    }

    // Burada başarılı GET dışı katalog işlemlerinden sonra ortak ürün cache etiketini ve Storefront sayfa önbelleğini geçersiz kılıyorum.
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

            try
            {
                await _revalidationService.RevalidateProductsAsync(CancellationToken.None);
                await _revalidationService.RevalidateCategoriesAsync(CancellationToken.None);
                await _revalidationService.RevalidateCollectionsAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Storefront revalidation bildirimi gönderilemedi.");
            }
        }
    }
}
