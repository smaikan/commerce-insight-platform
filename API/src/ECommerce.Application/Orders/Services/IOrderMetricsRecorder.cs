using ECommerce.Domain.Entities;

namespace ECommerce.Application.Orders.Services;

public interface IOrderMetricsRecorder
{
    // Burada tamamlanan satın alma miktarını ürün ve varyant metriklerine yansıtma sözleşmesini tanımlıyorum.
    Task RecordPurchasedQuantityAsync(
        Product product,
        ProductVariant variant,
        int quantity,
        CancellationToken cancellationToken = default);

    // Burada checkout satırlarının satın alma metriklerini toplu ve az sorgulu biçimde kaydetme sözleşmesini tanımlıyorum.
    Task RecordPurchasedQuantitiesAsync(
        IReadOnlyCollection<PurchaseMetricLine> lines,
        CancellationToken cancellationToken = default);
}

// Burada satın alma metriği yazılacak güvenilir ürün, varyant ve adet bilgisini taşıyorum.
public sealed record PurchaseMetricLine(Product Product, ProductVariant Variant, int Quantity);
