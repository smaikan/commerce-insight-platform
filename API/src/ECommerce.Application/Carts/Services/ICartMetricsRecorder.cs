using ECommerce.Domain.Entities;

namespace ECommerce.Application.Carts.Services;

public interface ICartMetricsRecorder
{
    // Burada gerçek sepete eklenen miktarı ürün ve günlük metriklere yansıtma sözleşmesini tanımlıyorum.
    Task RecordAddedQuantityAsync(
        Product product,
        ProductVariant variant,
        int quantity,
        CancellationToken cancellationToken = default);
}
