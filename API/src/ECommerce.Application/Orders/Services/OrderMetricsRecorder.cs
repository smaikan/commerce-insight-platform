using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Orders.Services;

public sealed class OrderMetricsRecorder : IOrderMetricsRecorder
{
    private readonly IProductEngagementRepository _engagementRepository;
    private readonly IDateTimeProvider _clock;

    // Burada satın alma sayaçları için gerekli repository ve güvenilir zaman kaynağını hazırlıyorum.
    public OrderMetricsRecorder(IProductEngagementRepository engagementRepository, IDateTimeProvider clock)
    {
        _engagementRepository = engagementRepository;
        _clock = clock;
    }

    // Burada satın alma miktarını ürün, varyant ve günlük metriklere aynı transaction içinde işliyorum.
    public async Task RecordPurchasedQuantityAsync(
        Product product,
        ProductVariant variant,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        await RecordPurchasedQuantitiesAsync(
            [new PurchaseMetricLine(product, variant, quantity)],
            cancellationToken);
    }

    // Burada tüm checkout satırlarının ürün, varyant ve günlük sayaçlarını toplu sorgularla güncelliyorum.
    public async Task RecordPurchasedQuantitiesAsync(
        IReadOnlyCollection<PurchaseMetricLine> lines,
        CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0)
        {
            return;
        }

        var date = DateOnly.FromDateTime(_clock.UtcNow);
        var productGroups = lines.GroupBy(line => line.Product.Id).ToList();
        var variantGroups = lines.GroupBy(line => line.Variant.Id).ToList();
        var productMetrics = (await _engagementRepository.GetProductDailyMetricsForUpdateAsync(
                productGroups.Select(group => group.Key),
                date,
                cancellationToken))
            .ToDictionary(metric => metric.ProductId);
        var variantMetrics = (await _engagementRepository.GetVariantDailyMetricsForUpdateAsync(
                variantGroups.Select(group => group.Key),
                date,
                cancellationToken))
            .ToDictionary(metric => metric.ProductVariantId);

        foreach (var productGroup in productGroups)
        {
            var quantity = SumQuantities(productGroup.Select(line => line.Quantity));
            var product = productGroup.First().Product;
            product.IncreaseTotalPurchaseCount(quantity);
            if (!productMetrics.TryGetValue(product.Id, out var productMetric))
            {
                productMetric = new ProductDailyMetric(product.Id, date);
                productMetrics.Add(product.Id, productMetric);
                await _engagementRepository.AddProductDailyMetricAsync(productMetric, cancellationToken);
            }

            productMetric.IncreasePurchaseCount(quantity);
        }

        foreach (var variantGroup in variantGroups)
        {
            var quantity = SumQuantities(variantGroup.Select(line => line.Quantity));
            var variant = variantGroup.First().Variant;
            variant.IncreasePurchaseCount(quantity);
            if (!variantMetrics.TryGetValue(variant.Id, out var variantMetric))
            {
                variantMetric = new ProductVariantDailyMetric(variant.Id, date);
                variantMetrics.Add(variant.Id, variantMetric);
                await _engagementRepository.AddVariantDailyMetricAsync(variantMetric, cancellationToken);
            }

            variantMetric.IncreasePurchaseCount(quantity);
        }
    }

    // Burada grup içindeki adetleri taşma riski olmadan integer sınırında topluyorum.
    private static int SumQuantities(IEnumerable<int> quantities)
    {
        try
        {
            return checked(quantities.Aggregate(0, (total, quantity) => total + quantity));
        }
        catch (OverflowException exception)
        {
            throw new InvalidOperationException("Purchase quantity exceeds the supported limit.", exception);
        }
    }
}
