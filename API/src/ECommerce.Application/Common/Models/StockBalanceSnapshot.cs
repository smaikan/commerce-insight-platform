namespace ECommerce.Application.Common.Models;

// Burada varyantın hızlı okuma bakiyesiyle hareket toplamını birlikte taşıyorum.
public sealed record StockBalanceSnapshot(
    Guid ProductVariantId,
    int PersistedStock,
    long MovementBalance);
