using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Orders.Services;

public sealed class OrderInventoryService
{
    private readonly IProductVariantRepository _variantRepository;

    // Burada siparişe bağlı stok hareketleri için varyant repository bağımlılığını hazırlıyorum.
    public OrderInventoryService(IProductVariantRepository variantRepository)
    {
        _variantRepository = variantRepository;
    }

    // Burada iptal edilmiş siparişin stoklarını kararlı varyant sırasıyla yalnız bir kez geri yüklüyorum.
    public async Task RestoreCancelledOrderStockAsync(Order order, CancellationToken cancellationToken)
    {
        var variants = await _variantRepository.GetByIdsForUpdateAsync(
            order.Items.Select(item => item.ProductVariantId),
            cancellationToken);
        var variantsById = variants.ToDictionary(variant => variant.Id);
        foreach (var item in order.Items.OrderBy(item => item.ProductVariantId))
        {
            if (!variantsById.TryGetValue(item.ProductVariantId, out var variant))
            {
                throw new ConflictException("A product variant required for stock restoration was not found.");
            }

            variant.ApplyStockMovement(
                item.Quantity,
                StockMovementType.Cancellation,
                "Order cancelled.",
                order.Id);
        }
    }
}
