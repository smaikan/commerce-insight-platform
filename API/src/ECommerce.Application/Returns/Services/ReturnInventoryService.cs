using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Returns.Services;

public sealed class ReturnInventoryService
{
    private readonly IProductVariantRepository _variantRepository;

    // Burada iade ve değişim stok hareketleri için varyant repository bağımlılığını hazırlıyorum.
    public ReturnInventoryService(IProductVariantRepository variantRepository)
    {
        _variantRepository = variantRepository;
    }

    // Burada kararı onaylanmış yeni refund veya teslim alınmış eski refund stoklarını transaction içinde yalnız bir kez geri ekliyorum.
    public async Task RestockRefundAsync(ReturnRequest returnRequest, CancellationToken cancellationToken)
    {
        if (returnRequest.Type != ReturnType.Refund || !CanApplyInventoryDecision(returnRequest))
        {
            throw new ConflictException("Only physically received approved refund requests can restore stock.");
        }

        var returnedQuantities = GetReturnedQuantities(returnRequest);
        var variants = await _variantRepository.GetByIdsForUpdateAsync(
            returnedQuantities.Keys,
            cancellationToken);
        var variantsById = variants.ToDictionary(variant => variant.Id);
        foreach (var returnedQuantity in returnedQuantities.OrderBy(item => item.Key))
        {
            if (!variantsById.TryGetValue(returnedQuantity.Key, out var variant))
            {
                throw new ConflictException("A product variant required for return stock restoration was not found.");
            }

            variant.ApplyStockMovement(
                returnedQuantity.Value,
                StockMovementType.SaleReturn,
                "Refund return approved.",
                returnRequest.OrderId,
                returnRequest.Id);
        }
    }

    // Burada onaylanan yeni veya tamamlanan eski değişimin iade girişini ve replacement çıkışını aynı transaction içinde uyguluyorum.
    public async Task FulfillExchangeAsync(ReturnRequest returnRequest, CancellationToken cancellationToken)
    {
        if (returnRequest.Type != ReturnType.Exchange || !CanApplyInventoryDecision(returnRequest))
        {
            throw new ConflictException("Only physically received approved exchange requests can fulfill replacement stock.");
        }

        var replacementIds = returnRequest.Items
            .Select(item => item.ReplacementProductVariantId)
            .OfType<Guid>()
            .ToList();
        if (replacementIds.Count != returnRequest.Items.Count)
        {
            throw new ConflictException("Every exchange item requires a replacement product variant.");
        }

        var trackedVariantIds = returnRequest.Items
            .Select(item => item.ProductVariantId)
            .Concat(replacementIds)
            .Distinct()
            .ToList();
        var variants = await _variantRepository.GetByIdsForUpdateAsync(trackedVariantIds, cancellationToken);
        var variantsById = variants.ToDictionary(variant => variant.Id);
        Dictionary<Guid, int> requiredReplacementQuantities;
        try
        {
            requiredReplacementQuantities = returnRequest.Items
                .GroupBy(item => item.ReplacementProductVariantId!.Value)
                .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        }
        catch (OverflowException exception)
        {
            throw new ConflictException(
                "Exchange replacement quantity exceeds the supported stock range.",
                exception);
        }

        foreach (var item in returnRequest.Items.OrderBy(item => item.ReplacementProductVariantId))
        {
            var replacementVariantId = item.ReplacementProductVariantId
                ?? throw new ConflictException("An exchange item is missing its replacement product variant.");
            if (!variantsById.TryGetValue(replacementVariantId, out var replacementVariant))
            {
                throw new ConflictException("A replacement product variant was not found.");
            }

            if (replacementVariant.ProductId != item.ProductId)
            {
                throw new ConflictException("An exchange replacement must belong to the same product.");
            }

            if (!replacementVariant.IsActive)
            {
                throw new ConflictException("An exchange replacement product variant is not active.");
            }

            if (replacementVariant.NetPrice != item.UnitPrice)
            {
                throw new ConflictException("An exchange replacement product variant price no longer matches the returned item.");
            }

            if (replacementVariant.Stock < requiredReplacementQuantities[replacementVariantId])
            {
                throw new ConflictException("An exchange replacement product variant does not have enough stock.");
            }
        }

        var returnedQuantities = GetReturnedQuantities(returnRequest);
        foreach (var returnedQuantity in returnedQuantities.OrderBy(item => item.Key))
        {
            if (!variantsById.TryGetValue(returnedQuantity.Key, out var returnedVariant))
            {
                throw new ConflictException("A product variant required for return stock restoration was not found.");
            }

            returnedVariant.ApplyStockMovement(
                returnedQuantity.Value,
                StockMovementType.SaleReturn,
                "Exchange return approved and fulfilled.",
                returnRequest.OrderId,
                returnRequest.Id);
        }

        foreach (var replacement in requiredReplacementQuantities.OrderBy(item => item.Key))
        {
            var replacementVariant = variantsById[replacement.Key];
            replacementVariant.ApplyStockMovement(
                -replacement.Value,
                StockMovementType.Sale,
                "Exchange replacement fulfilled.",
                returnRequest.OrderId,
                returnRequest.Id);
        }
    }

    // Burada stok etkisinin yalnız yeni teslim-sonrası onayda veya eski onay-önce completion dalında çalışmasına izin veriyorum.
    private static bool CanApplyInventoryDecision(ReturnRequest returnRequest)
    {
        return returnRequest.ReceivedAt.HasValue &&
               returnRequest.ApprovedAt.HasValue &&
               (returnRequest.Status == ReturnRequestStatus.Approved ||
                returnRequest.IsLegacyReceivedAwaitingCompletion() ||
                (returnRequest.Status == ReturnRequestStatus.Completed && returnRequest.CompletedAt.HasValue));
    }

    // Burada aynı varyanta ait iade kalemlerini tek stok hareketine güvenli biçimde topluyorum.
    private static IReadOnlyDictionary<Guid, int> GetReturnedQuantities(ReturnRequest returnRequest)
    {
        try
        {
            return returnRequest.Items
                .GroupBy(item => item.ProductVariantId)
                .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        }
        catch (OverflowException exception)
        {
            throw new ConflictException(
                "Returned quantity exceeds the supported stock range.",
                exception);
        }
    }
}
