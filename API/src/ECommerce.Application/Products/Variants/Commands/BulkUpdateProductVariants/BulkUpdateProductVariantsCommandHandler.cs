using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Products.Variants.Commands.BulkUpdateProductVariants;

public sealed class BulkUpdateProductVariantsCommandHandler
    : IRequestHandler<BulkUpdateProductVariantsCommand, IReadOnlyList<ProductVariantDto>>
{
    private const string TemporarySkuPrefix = "__BULK__";
    private const int TemporarySkuGenerationAttempts = 5;
    private readonly IProductVariantRepository _variantRepository;
    private readonly IVariantOptionResolver _variantOptionResolver;
    private readonly IUnitOfWork _unitOfWork;

    // Burada atomik varyant güncellemesi için repository, seçenek çözücü ve transaction bağımlılıklarını hazırlıyorum.
    public BulkUpdateProductVariantsCommandHandler(
        IProductVariantRepository variantRepository,
        IVariantOptionResolver variantOptionResolver,
        IUnitOfWork unitOfWork)
    {
        _variantRepository = variantRepository;
        _variantOptionResolver = variantOptionResolver;
        _unitOfWork = unitOfWork;
    }

    // Burada batch güncellemesini tek serializable transaction sınırında çalıştırıyorum.
    public Task<IReadOnlyList<ProductVariantDto>> Handle(
        BulkUpdateProductVariantsCommand request,
        CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCancellationToken => UpdateVariantsAsync(request, transactionCancellationToken),
            cancellationToken);
    }

    // Burada bütün satırları doğrulayıp SKU değişimini iki aşamada, diğer alanları ise yalnız son aşamada uyguluyorum.
    private async Task<IReadOnlyList<ProductVariantDto>> UpdateVariantsAsync(
        BulkUpdateProductVariantsCommand request,
        CancellationToken cancellationToken)
    {
        var requestedIds = request.Variants
            .Select(item => item.Id)
            .OrderBy(id => id)
            .ToArray();
        var variants = await _variantRepository.GetByIdsWithDetailsForUpdateAsync(
            requestedIds,
            cancellationToken);

        if (variants.Count != requestedIds.Length || variants.Any(variant => variant.ProductId != request.ProductId))
        {
            throw new NotFoundException("One or more product variants were not found for this product.");
        }

        var variantsById = variants.ToDictionary(variant => variant.Id);
        if (request.Variants.Any(item =>
                variantsById[item.Id].ConcurrencyToken != item.ExpectedConcurrencyToken))
        {
            throw new ConcurrencyException(
                "One or more product variants were changed by another operation. Refresh the data and try again.");
        }

        await ThrowIfTargetSkuConflictsAsync(request, requestedIds, cancellationToken);

        var resolvedOptions = new Dictionary<Guid, IReadOnlyList<VariantOptionSelection>>();
        foreach (var item in request.Variants)
        {
            cancellationToken.ThrowIfCancellationRequested();
            resolvedOptions[item.Id] = await _variantOptionResolver.ResolveCompositeAsync(
                item.Name,
                item.Value,
                cancellationToken);
        }

        var temporarySkus = await CreateAvailableTemporarySkusAsync(
            variants,
            request.Variants.Select(item => item.Sku),
            requestedIds,
            cancellationToken);
        foreach (var variant in variants)
        {
            variant.ChangeSku(temporarySkus[variant.Id]);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var item in request.Variants)
        {
            ApplyFinalValues(
                variantsById[item.Id],
                item,
                resolvedOptions[item.Id]);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return request.Variants
            .Select(item => variantsById[item.Id].ToDto())
            .ToList();
    }

    // Burada hedef SKU'ların yalnız batch dışındaki mevcut varyantlarla çakışmasını alan bazlı hata olarak bildiriyorum.
    private async Task ThrowIfTargetSkuConflictsAsync(
        BulkUpdateProductVariantsCommand request,
        IReadOnlyCollection<Guid> requestedIds,
        CancellationToken cancellationToken)
    {
        var existingSkus = await _variantRepository.GetExistingSkusAsync(
            request.Variants.Select(item => item.Sku),
            requestedIds,
            cancellationToken);
        if (existingSkus.Count == 0)
        {
            return;
        }

        var conflicts = new HashSet<string>(existingSkus, StringComparer.OrdinalIgnoreCase);
        var errors = request.Variants
            .Select((item, index) => (item, index))
            .Where(entry => conflicts.Contains(entry.item.Sku.Trim()))
            .ToDictionary(
                entry => $"variants[{entry.index}].sku",
                _ => new[] { "This SKU is already used by a variant outside this batch." });

        throw new ProductVariantSkuConflictException(errors);
    }

    // Burada global unique index ile çakışmayacak kısa ve benzersiz geçici SKU değerleri üretiyorum.
    private async Task<IReadOnlyDictionary<Guid, string>> CreateAvailableTemporarySkusAsync(
        IReadOnlyList<ProductVariant> variants,
        IEnumerable<string> targetSkus,
        IReadOnlyCollection<Guid> requestedIds,
        CancellationToken cancellationToken)
    {
        var reservedSkus = new HashSet<string>(
            variants.Select(variant => variant.Sku).Concat(targetSkus.Select(sku => sku.Trim())),
            StringComparer.OrdinalIgnoreCase);

        for (var attempt = 0; attempt < TemporarySkuGenerationAttempts; attempt++)
        {
            var candidates = variants.ToDictionary(
                variant => variant.Id,
                _ => $"{TemporarySkuPrefix}{Guid.NewGuid():N}");
            if (candidates.Values.Any(reservedSkus.Contains))
            {
                continue;
            }

            var conflicts = await _variantRepository.GetExistingSkusAsync(
                candidates.Values,
                requestedIds,
                cancellationToken);
            if (conflicts.Count == 0)
            {
                return candidates;
            }
        }

        throw new InvalidOperationException("Unique temporary SKU values could not be reserved for this batch.");
    }

    // Burada tek varyantın nihai detay, seçenek, fiyat, stok ve aktivasyon durumunu mevcut domain kurallarıyla uyguluyorum.
    private static void ApplyFinalValues(
        ProductVariant variant,
        BulkUpdateProductVariantItem item,
        IReadOnlyList<VariantOptionSelection> resolvedOptions)
    {
        variant.UpdateDetails(
            item.Name,
            item.Value,
            item.Sku,
            item.Barcode,
            item.Material);
        if (!HasSameOptionValues(variant, resolvedOptions))
        {
            variant.ReplaceOptionValues(resolvedOptions);
        }

        variant.UpdatePrice(
            item.Price,
            item.CompareAtPrice,
            variant.Product.TaxRate?.CalculateNetPrice(item.Price) ?? item.Price);

        var stockDifference = item.Stock - variant.Stock;
        if (stockDifference != 0)
        {
            variant.ApplyStockMovement(
                stockDifference,
                StockMovementType.StockCountAdjustment,
                item.StockAdjustmentReason ?? "Variant stock count updated by bulk operation.");
        }

        if (item.IsActive)
        {
            variant.Activate();
        }
        else
        {
            variant.Deactivate();
        }
    }

    // Burada aynı seçenek bağlantılarını gereksiz silip yeniden eklemekten kaçınıyorum.
    private static bool HasSameOptionValues(
        ProductVariant variant,
        IReadOnlyList<VariantOptionSelection> resolvedOptions)
    {
        var existing = variant.OptionValues
            .Select(item => (item.VariantOptionNameId, item.VariantOptionValueId))
            .OrderBy(item => item.VariantOptionNameId)
            .ThenBy(item => item.VariantOptionValueId);
        var resolved = resolvedOptions
            .Select(item => (OptionNameId: item.Name.Id, OptionValueId: item.Value.Id))
            .OrderBy(item => item.OptionNameId)
            .ThenBy(item => item.OptionValueId);

        return existing.SequenceEqual(resolved);
    }
}
