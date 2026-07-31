using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class StockMovement : BaseEntity
{
    public const int MaximumReasonLength = 500;

    public Guid ProductVariantId { get; private set; }
    public ProductVariant ProductVariant { get; private set; } = null!;
    public StockMovementDirection Direction { get; private set; }
    public StockMovementType Type { get; private set; }
    public int QuantityDelta { get; private set; }
    public int StockBeforeMovement { get; private set; }
    public int StockAfterMovement { get; private set; }
    public string? Reason { get; private set; }
    public Guid? OrderId { get; private set; }
    public Guid? ReturnRequestId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Burada EF Core'un stok hareketini veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private StockMovement()
    {
    }

    // Burada yalnızca varyant aggregate'i tarafından üretilebilen doğrulanmış stok hareketini oluşturuyorum.
    internal StockMovement(
        ProductVariant productVariant,
        int quantityDelta,
        StockMovementType type,
        string? reason = null,
        Guid? orderId = null,
        Guid? returnRequestId = null)
    {
        ArgumentNullException.ThrowIfNull(productVariant);

        if (quantityDelta == 0)
        {
            throw new DomainException("Stock movement quantity delta cannot be zero.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new DomainException("Stock movement type is invalid.");
        }

        if (orderId == Guid.Empty)
        {
            throw new DomainException("Order id cannot be empty.");
        }

        if (returnRequestId == Guid.Empty)
        {
            throw new DomainException("Return request id cannot be empty.");
        }

        Direction = quantityDelta > 0
            ? StockMovementDirection.In
            : StockMovementDirection.Out;

        EnsureTypeMatchesDirection(type, Direction);
        EnsureRequiredReference(type, orderId, returnRequestId);

        var stockAfterMovement = (long)productVariant.Stock + quantityDelta;
        if (stockAfterMovement < 0)
        {
            throw new DomainException("Stock cannot be negative.");
        }

        if (stockAfterMovement > int.MaxValue)
        {
            throw new DomainException("Stock cannot exceed the maximum supported value.");
        }

        ProductVariantId = productVariant.Id;
        ProductVariant = productVariant;
        Type = type;
        QuantityDelta = quantityDelta;
        StockBeforeMovement = productVariant.Stock;
        StockAfterMovement = (int)stockAfterMovement;
        Reason = NormalizeReason(reason);
        OrderId = orderId;
        ReturnRequestId = returnRequestId;
        CreatedAt = DateTime.UtcNow;
    }

    // Burada hareket türünün miktardan türetilen giriş veya çıkış yönüyle uyumunu koruyorum.
    private static void EnsureTypeMatchesDirection(
        StockMovementType type,
        StockMovementDirection direction)
    {
        var isDirectionValid = type switch
        {
            StockMovementType.OpeningBalance => direction == StockMovementDirection.In,
            StockMovementType.Purchase => direction == StockMovementDirection.In,
            StockMovementType.PurchaseReturn => direction == StockMovementDirection.Out,
            StockMovementType.Sale => direction == StockMovementDirection.Out,
            StockMovementType.SaleReturn => direction == StockMovementDirection.In,
            StockMovementType.AccountingSale => direction == StockMovementDirection.Out,
            StockMovementType.AccountingSaleCancellation => direction == StockMovementDirection.In,
            StockMovementType.ManualAdjustment => true,
            StockMovementType.StockCountAdjustment => true,
            StockMovementType.Loss => direction == StockMovementDirection.Out,
            StockMovementType.Damage => direction == StockMovementDirection.Out,
            StockMovementType.Expired => direction == StockMovementDirection.Out,
            StockMovementType.TransferIn => direction == StockMovementDirection.In,
            StockMovementType.TransferOut => direction == StockMovementDirection.Out,
            StockMovementType.Cancellation => direction == StockMovementDirection.In,
            _ => false
        };

        if (!isDirectionValid)
        {
            throw new DomainException("Stock movement type does not match its direction.");
        }
    }

    // Burada satış ve iade hareketlerinin ilgili iş kaydına bağlı olmasını zorunlu tutuyorum.
    private static void EnsureRequiredReference(
        StockMovementType type,
        Guid? orderId,
        Guid? returnRequestId)
    {
        if (type is StockMovementType.Sale or StockMovementType.Cancellation &&
            !orderId.HasValue)
        {
            throw new DomainException("Sale and cancellation stock movements must reference an order.");
        }

        if (type == StockMovementType.SaleReturn && !returnRequestId.HasValue)
        {
            throw new DomainException("Sale return stock movement must reference a return request.");
        }

        if (type is StockMovementType.AccountingSale or StockMovementType.AccountingSaleCancellation &&
            (orderId.HasValue || returnRequestId.HasValue))
        {
            throw new DomainException(
                "Accounting sale stock movements use the accounting mapping and cannot reference an e-commerce order or return.");
        }
    }

    // Burada isteğe bağlı stok hareketi nedenini temizleyip uzunluk sınırını uyguluyorum.
    private static string? NormalizeReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        var normalizedReason = reason.Trim();
        if (normalizedReason.Length > MaximumReasonLength)
        {
            throw new DomainException($"Stock movement reason cannot exceed {MaximumReasonLength} characters.");
        }

        return normalizedReason;
    }
}
