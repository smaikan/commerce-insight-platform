using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Carts.Services;

public sealed class CartMetricsRecorder : ICartMetricsRecorder
{
    private readonly IProductEngagementRepository _engagementRepository;
    private readonly IDateTimeProvider _clock;

    // Burada sepete ekleme metrikleri için repository ve zaman kaynağını hazırlıyorum.
    public CartMetricsRecorder(
        IProductEngagementRepository engagementRepository,
        IDateTimeProvider clock)
    {
        _engagementRepository = engagementRepository;
        _clock = clock;
    }

    // Burada eklenen miktarı ürün, varyant ve günlük sayaçlara aynı işlem içinde uyguluyorum.
    public async Task RecordAddedQuantityAsync(
        Product product,
        ProductVariant variant,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        product.IncreaseTotalAddToCartCount(quantity);
        variant.IncreaseAddToCartCount(quantity);

        var date = DateOnly.FromDateTime(_clock.UtcNow);
        var productMetric = await _engagementRepository.GetProductDailyMetricForUpdateAsync(
            product.Id,
            date,
            cancellationToken);
        if (productMetric is null)
        {
            productMetric = new ProductDailyMetric(product.Id, date);
            await _engagementRepository.AddProductDailyMetricAsync(productMetric, cancellationToken);
        }

        var variantMetric = await _engagementRepository.GetVariantDailyMetricForUpdateAsync(
            variant.Id,
            date,
            cancellationToken);
        if (variantMetric is null)
        {
            variantMetric = new ProductVariantDailyMetric(variant.Id, date);
            await _engagementRepository.AddVariantDailyMetricAsync(variantMetric, cancellationToken);
        }

        productMetric.IncreaseAddToCartCount(quantity);
        variantMetric.IncreaseAddToCartCount(quantity);
    }
}
