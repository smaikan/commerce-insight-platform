using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Accounting.CostLayers;

// Burada OpeningBalance maliyet katmanının API tarafından okunabilir maliyet, miktar ve eşzamanlılık alanlarını taşıyorum.
public sealed record OpeningBalanceCostLayerDto(
    Guid Id,
    Guid ProductVariantId,
    Guid StockMovementId,
    InventoryCostLayerSourceType SourceType,
    int OriginalQuantity,
    int RemainingQuantity,
    decimal UnitCostExcludingVat,
    decimal UnitCostIncludingVat,
    decimal TotalCostExcludingVat,
    decimal TotalCostIncludingVat,
    DateTime CostDate,
    CostLayerStatus Status,
    Guid ConcurrencyToken);

// Burada yeni varyantı kendi opsiyonel açılış maliyetleriyle eşleyerek katman yazıcısına taşıyorum.
public sealed record OpeningBalanceCostLayerSeed(
    ProductVariant Variant,
    decimal? OpeningUnitCostExcludingVat = null,
    decimal? OpeningUnitCostIncludingVat = null);

public interface IOpeningBalanceCostLayerRepository
{
    // Burada yeni OpeningBalance katmanını aynı DbContext takibine ekleme sözleşmesini tanımlıyorum.
    void Add(InventoryCostLayer layer);

    // Burada OpeningBalance hareketleri için daha önce katman oluşturulmuş kimlikleri topluca sorguluyorum.
    Task<IReadOnlySet<Guid>> GetExistingStockMovementIdsAsync(
        IEnumerable<Guid> stockMovementIds,
        CancellationToken cancellationToken = default);

    // Burada OpeningBalance katmanını eşzamanlı güncelleme için takipli getirme sözleşmesini tanımlıyorum.
    Task<InventoryCostLayer?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // Burada varyantın OpeningBalance katmanını detay okuması için takip etmeden getiriyorum.
    Task<InventoryCostLayer?> GetByProductVariantIdAsync(
        Guid productVariantId,
        CancellationToken cancellationToken = default);
}

public interface IOpeningBalanceCostLayerWriter
{
    // Burada yeni varyantların pozitif OpeningBalance hareketleri için varsayılan sıfır maliyet katmanı hazırlıyorum.
    Task CreateForNewVariantsAsync(
        IEnumerable<ProductVariant> variants,
        CancellationToken cancellationToken = default);

    // Burada yeni varyantların pozitif OpeningBalance hareketleri için gönderilen opsiyonel maliyetlerle katman hazırlıyorum.
    Task CreateForNewVariantsAsync(
        IEnumerable<OpeningBalanceCostLayerSeed> seeds,
        CancellationToken cancellationToken = default);
}
