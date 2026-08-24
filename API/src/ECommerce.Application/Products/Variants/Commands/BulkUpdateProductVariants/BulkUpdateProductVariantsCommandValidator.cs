using FluentValidation;

namespace ECommerce.Application.Products.Variants.Commands.BulkUpdateProductVariants;

public sealed class BulkUpdateProductVariantsCommandValidator
    : AbstractValidator<BulkUpdateProductVariantsCommand>
{
    // Burada batch boyutunu, ürün kimliğini ve satırlar arası benzersizlik kurallarını doğruluyorum.
    public BulkUpdateProductVariantsCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .GreaterThan(0);

        RuleFor(command => command.Variants)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .NotEmpty()
            .Must(variants => variants.Count <= BulkUpdateProductVariantsCommand.MaximumBatchSize)
            .WithMessage($"A variant batch cannot contain more than {BulkUpdateProductVariantsCommand.MaximumBatchSize} items.");

        RuleFor(command => command.Variants)
            .Must(HasDistinctVariantIds)
            .WithMessage("The same product variant cannot occur more than once in a batch.")
            .When(command => command.Variants is { Count: > 0 });

        RuleFor(command => command.Variants)
            .Must(HasDistinctTargetSkus)
            .WithMessage("Target SKU values must be unique within the batch.")
            .When(command => command.Variants is { Count: > 0 });

        RuleForEach(command => command.Variants)
            .NotNull()
            .SetValidator(new BulkUpdateProductVariantItemValidator());
    }

    // Burada aynı varyant kimliğinin batch içinde yinelenmesini engelliyorum.
    private static bool HasDistinctVariantIds(IReadOnlyList<BulkUpdateProductVariantItem> variants) =>
        variants.Select(variant => variant.Id).Distinct().Count() == variants.Count;

    // Burada boşlukları temizlenmiş hedef SKU değerlerini büyük-küçük harf duyarsız karşılaştırıyorum.
    private static bool HasDistinctTargetSkus(IReadOnlyList<BulkUpdateProductVariantItem> variants) =>
        variants.Select(variant => variant.Sku?.Trim() ?? string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == variants.Count;
}

internal sealed class BulkUpdateProductVariantItemValidator
    : AbstractValidator<BulkUpdateProductVariantItem>
{
    // Burada tek batch satırının varyant, fiyat, stok, seçenek ve concurrency alanlarını doğruluyorum.
    public BulkUpdateProductVariantItemValidator()
    {
        RuleFor(item => item.Id)
            .NotEmpty();

        RuleFor(item => item.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(item => item.Value)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(item => item)
            .Must(HasMatchingOptionParts)
            .WithMessage("Variant name and value must contain one to three matching unique parts.");

        RuleFor(item => item.Sku)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(item => item.Price)
            .GreaterThan(0);

        RuleFor(item => item.CompareAtPrice)
            .GreaterThanOrEqualTo(item => item.Price)
            .When(item => item.CompareAtPrice.HasValue);

        RuleFor(item => item.Stock)
            .GreaterThanOrEqualTo(0);

        RuleFor(item => item.ExpectedConcurrencyToken)
            .NotEmpty();

        RuleFor(item => item.Barcode)
            .MaximumLength(100);

        RuleFor(item => item.Material)
            .MaximumLength(120);

        RuleFor(item => item.StockAdjustmentReason)
            .MaximumLength(500);
    }

    // Burada birleşik seçenek adlarıyla değerlerinin parça sayısı ve tekillik bakımından eşleşmesini doğruluyorum.
    private static bool HasMatchingOptionParts(BulkUpdateProductVariantItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.Value))
        {
            return true;
        }

        var names = item.Name.Split('/').Select(part => part.Trim()).ToArray();
        var values = item.Value.Split('/').Select(part => part.Trim()).ToArray();
        return names.Length is >= 1 and <= 3 &&
               names.Length == values.Length &&
               names.All(part => !string.IsNullOrWhiteSpace(part)) &&
               values.All(part => !string.IsNullOrWhiteSpace(part)) &&
               names.Distinct(StringComparer.Ordinal).Count() == names.Length;
    }
}
