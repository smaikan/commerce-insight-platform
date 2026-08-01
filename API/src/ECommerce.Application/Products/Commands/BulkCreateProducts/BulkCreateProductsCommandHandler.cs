using ECommerce.Application.Accounting.CostLayers;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Products.Commands.BulkCreateProducts;

public sealed class BulkCreateProductsCommandHandler : IRequestHandler<BulkCreateProductsCommand, IReadOnlyList<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductTypeRepository _productTypeRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly ITaxRateRepository _taxRateRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IProductTagResolver _productTagResolver;
    private readonly IProductUrlGenerator _productUrlGenerator;

    private readonly IProductUrlResolver _productUrlResolver;
    private readonly IOpeningBalanceCostLayerWriter _openingBalanceCostLayerWriter;
    private readonly IVariantOptionResolver? _variantOptionResolver;
    private readonly IUnitOfWork _unitOfWork;

    // Burada toplu ürün oluşturma akışının ihtiyaç duyduğu bağımlılıkları hazırlıyorum.
    public BulkCreateProductsCommandHandler(
        IProductRepository productRepository,
        IProductTypeRepository productTypeRepository,
        IBrandRepository brandRepository,
        ITaxRateRepository taxRateRepository,
        ICollectionRepository collectionRepository,
        ITagRepository tagRepository,
        IProductTagResolver productTagResolver,
        IProductUrlGenerator productUrlGenerator,

        IOpeningBalanceCostLayerWriter openingBalanceCostLayerWriter,
        IUnitOfWork unitOfWork,
        IVariantOptionResolver? variantOptionResolver = null,
        IProductUrlResolver? productUrlResolver = null)
    {
        _productRepository = productRepository;
        _productTypeRepository = productTypeRepository;
        _brandRepository = brandRepository;
        _taxRateRepository = taxRateRepository;
        _collectionRepository = collectionRepository;
        _tagRepository = tagRepository;
        _productTagResolver = productTagResolver;
        _productUrlGenerator = productUrlGenerator;

        _productUrlResolver = productUrlResolver ?? new ProductUrlResolver(productRepository, productUrlGenerator);
        _openingBalanceCostLayerWriter = openingBalanceCostLayerWriter;
        _variantOptionResolver = variantOptionResolver;
        _unitOfWork = unitOfWork;
    }

    // Burada toplu ürün yükleme isteğini kontrol edip ürünleri ilişkileriyle birlikte oluşturuyorum.
    public async Task<IReadOnlyList<ProductDto>> Handle(BulkCreateProductsCommand request, CancellationToken cancellationToken)
    {
        var items = request.Products;
        var preparedItems = new List<PreparedProductItem>(items.Count);
        var requestReservedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var resolvedUrl = await _productUrlResolver.ResolveAsync(
                item.Title,
                item.Url,
                requestReservedUrls: requestReservedUrls,
                cancellationToken: cancellationToken);
            preparedItems.Add(new PreparedProductItem(
                item,
                resolvedUrl,
                item.MainSku.Trim().ToUpperInvariant()));
        }

        await EnsureMainSkusAreUniqueAsync(preparedItems, cancellationToken);

        await EnsureProductTypesExistAsync(preparedItems, cancellationToken);
        await EnsureBrandsExistAsync(preparedItems, cancellationToken);
        var taxRatesById = await GetActiveTaxRatesAsync(preparedItems, cancellationToken);
        await EnsureCollectionsExistAsync(preparedItems, cancellationToken);
        await EnsureTagsExistAsync(preparedItems, cancellationToken);
        var resolvedTags = await ResolveTagsAsync(preparedItems, cancellationToken);
        await EnsureVariantSkusAreUniqueAsync(preparedItems, cancellationToken);

        var products = preparedItems
            .Select(item => CreateProduct(item, resolvedTags, taxRatesById))
            .ToList();
        if (_variantOptionResolver is not null)
        {
            await AssignVariantOptionsAsync(products, preparedItems, cancellationToken);
        }
        var openingBalanceSeeds = products
            .Zip(
                preparedItems,
                (product, preparedItem) => product.Variants.Zip(
                    preparedItem.Item.Variants ??
                        Array.Empty<BulkCreateProductVariantItem>(),
                    (variant, item) => new OpeningBalanceCostLayerSeed(
                        variant,
                        item.OpeningUnitCostExcludingVat,
                        item.OpeningUnitCostIncludingVat)))
            .SelectMany(seeds => seeds)
            .ToArray();

        await _productRepository.AddRangeAsync(products, cancellationToken);
        await _openingBalanceCostLayerWriter.CreateForNewVariantsAsync(
            openingBalanceSeeds,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var productIds = products.Select(product => product.Id).ToList();
        var createdProducts = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        var createdProductMap = createdProducts.ToDictionary(product => product.Id);

        return productIds
            .Select(id => createdProductMap.TryGetValue(id, out var product) ? product.ToDto() : products.Single(product => product.Id == id).ToDto())
            .ToList();
    }

    // Burada toplu istekte kullanılan ürün türlerinin veritabanında bulunduğunu doğruluyorum.
    private async Task EnsureProductTypesExistAsync(
        IReadOnlyCollection<PreparedProductItem> preparedItems,
        CancellationToken cancellationToken)
    {
        var typeIds = preparedItems
            .Select(item => item.Item.TypeId)
            .Where(typeId => typeId.HasValue)
            .Select(typeId => typeId!.Value)
            .Distinct()
            .ToList();

        if (typeIds.Count == 0)
        {
            return;
        }

        var existingTypeIds = await _productTypeRepository.GetExistingIdsAsync(typeIds, cancellationToken);
        var missingTypeIds = typeIds
            .Where(typeId => !existingTypeIds.Contains(typeId))
            .ToList();

        if (missingTypeIds.Count > 0)
        {
            throw new NotFoundException($"Product type was not found: {string.Join(", ", missingTypeIds)}.");
        }
    }

    // Burada toplu istekte kullanılan markaların veritabanında bulunduğunu doğruluyorum.
    private async Task EnsureBrandsExistAsync(
        IReadOnlyCollection<PreparedProductItem> preparedItems,
        CancellationToken cancellationToken)
    {
        var brandIds = preparedItems
            .Select(item => item.Item.BrandId)
            .Where(brandId => brandId.HasValue)
            .Select(brandId => brandId!.Value)
            .Distinct()
            .ToList();

        if (brandIds.Count == 0)
        {
            return;
        }

        var existingBrandIds = await _brandRepository.GetExistingIdsAsync(brandIds, cancellationToken);
        var missingBrandIds = brandIds
            .Where(brandId => !existingBrandIds.Contains(brandId))
            .ToList();

        if (missingBrandIds.Count > 0)
        {
            throw new NotFoundException($"Brand was not found: {string.Join(", ", missingBrandIds)}.");
        }
    }

    // Burada toplu istekte seçilen vergi oranlarının aktif olarak bulunduğunu doğruluyorum.
    private async Task<IReadOnlyDictionary<Guid, TaxRate>> GetActiveTaxRatesAsync(
        IReadOnlyCollection<PreparedProductItem> preparedItems,
        CancellationToken cancellationToken)
    {
        var taxRateIds = preparedItems
            .Select(item => item.Item.TaxRateId)
            .Where(taxRateId => taxRateId.HasValue)
            .Select(taxRateId => taxRateId!.Value)
            .Distinct()
            .ToList();

        if (taxRateIds.Count == 0)
        {
            return new Dictionary<Guid, TaxRate>();
        }

        var taxRatesById = await _taxRateRepository.GetActiveByIdsAsync(taxRateIds, cancellationToken);
        var missingTaxRateIds = taxRateIds
            .Where(taxRateId => !taxRatesById.ContainsKey(taxRateId))
            .ToList();

        if (missingTaxRateIds.Count > 0)
        {
            throw new NotFoundException($"Tax rate was not found or is inactive: {string.Join(", ", missingTaxRateIds)}.");
        }

        return taxRatesById;
    }

    // Burada toplu istekte kullanılan koleksiyonların veritabanında bulunduğunu doğruluyorum.
    private async Task EnsureCollectionsExistAsync(
        IReadOnlyCollection<PreparedProductItem> preparedItems,
        CancellationToken cancellationToken)
    {
        var collectionIds = preparedItems
            .SelectMany(item => item.Item.CollectionIds ?? Array.Empty<Guid>())
            .Distinct()
            .ToList();

        if (collectionIds.Count == 0)
        {
            return;
        }

        var existingCollectionIds = await _collectionRepository.GetExistingIdsAsync(collectionIds, cancellationToken);
        var missingCollectionIds = collectionIds
            .Where(collectionId => !existingCollectionIds.Contains(collectionId))
            .ToList();

        if (missingCollectionIds.Count > 0)
        {
            throw new NotFoundException($"Collection was not found: {string.Join(", ", missingCollectionIds)}.");
        }
    }

    // Burada toplu istekte kullanılan etiketlerin veritabanında bulunduğunu doğruluyorum.
    private async Task EnsureTagsExistAsync(
        IReadOnlyCollection<PreparedProductItem> preparedItems,
        CancellationToken cancellationToken)
    {
        var tagIds = preparedItems
            .SelectMany(item => item.Item.TagIds ?? Array.Empty<Guid>())
            .Distinct()
            .ToList();

        if (tagIds.Count == 0)
        {
            return;
        }

        var existingTagIds = await _tagRepository.GetExistingIdsAsync(tagIds, cancellationToken);
        var missingTagIds = tagIds
            .Where(tagId => !existingTagIds.Contains(tagId))
            .ToList();

        if (missingTagIds.Count > 0)
        {
            throw new NotFoundException($"Tag was not found: {string.Join(", ", missingTagIds)}.");
        }
    }

    // Burada toplu istekteki ana SKU değerlerinin istek içinde ve veritabanında benzersiz olduğunu doğruluyorum.
    private async Task EnsureMainSkusAreUniqueAsync(
        IReadOnlyCollection<PreparedProductItem> preparedItems,
        CancellationToken cancellationToken)
    {
        var mainSkus = preparedItems
            .Select(item => item.MainSku)
            .ToList();

        EnsureNoDuplicates(mainSkus, "Product main SKU is duplicated in the request.");

        var existingMainSkus = await _productRepository.GetExistingMainSkusAsync(mainSkus, cancellationToken);
        if (existingMainSkus.Count > 0)
        {
            throw new ConflictException(
                $"Product main SKU already exists: {string.Join(", ", existingMainSkus)}.");
        }
    }

    // Burada ürünlerin adla gönderilen etiketlerini mevcut veya yeni kayıtlara topluca çözümlüyorum.
    private async Task<ProductTagResolution> ResolveTagsAsync(
        IReadOnlyCollection<PreparedProductItem> preparedItems,
        CancellationToken cancellationToken)
    {
        var tagNames = preparedItems
            .SelectMany(item => item.Item.Tags ?? Array.Empty<string>())
            .ToList();

        return tagNames.Count == 0
            ? ProductTagResolution.Empty
            : await _productTagResolver.ResolveAsync(tagNames, cancellationToken);
    }

    // Burada toplu istekteki varyant SKU değerlerinin istek içinde ve veritabanında benzersiz olduğunu doğruluyorum.
    private async Task EnsureVariantSkusAreUniqueAsync(
        IReadOnlyCollection<PreparedProductItem> preparedItems,
        CancellationToken cancellationToken)
    {
        var skus = preparedItems
            .SelectMany(item => item.Item.Variants ?? Array.Empty<BulkCreateProductVariantItem>())
            .Select(variant => variant.Sku.Trim())
            .ToList();

        EnsureNoDuplicates(skus, "Variant SKU is duplicated in the request.");

        var existingSkus = await _productRepository.GetExistingVariantSkusAsync(skus, cancellationToken);

        if (existingSkus.Count > 0)
        {
            throw new ConflictException($"Variant SKU already exists: {string.Join(", ", existingSkus)}.");
        }
    }

    // Burada hazırlanmış toplu istek satırından ilişkileriyle birlikte ürün aggregate'ı oluşturuyorum.
    private static Product CreateProduct(
        PreparedProductItem preparedItem,
        ProductTagResolution resolvedTags,
        IReadOnlyDictionary<Guid, TaxRate> taxRatesById)
    {
        var item = preparedItem.Item;
        var taxRate = item.TaxRateId.HasValue ? taxRatesById[item.TaxRateId.Value] : null;
        var product = new Product(
            item.Title,
            preparedItem.Url,
            preparedItem.MainSku,
            item.TypeId,
            item.BrandId,
            item.Description,
            item.Status,
            item.IsActive,
            item.IsFeatured,
            item.DisplayOrder,
            item.SeoTitle,
            item.SeoDescription,
            item.TaxRateId);

        foreach (var variant in item.Variants ?? Array.Empty<BulkCreateProductVariantItem>())
        {
            var productVariant = new ProductVariant(
                product,
                variant.Name,
                variant.Sku,
                variant.Price,
                variant.Stock,
                variant.CompareAtPrice,
                variant.Barcode,
                variant.Material,
                variant.IsActive,
                taxRate?.CalculateNetPrice(variant.Price) ?? variant.Price,
                variant.Value);

            product.Variants.Add(productVariant);
        }

        foreach (var image in item.Images ?? Array.Empty<BulkCreateProductImageItem>())
        {
            product.Images.Add(new ProductImage(
                product,
                image.ImageUrl,
                image.DisplayOrder,
                image.IsMain,
                image.AltText));
        }

        foreach (var collectionId in item.CollectionIds?.Distinct() ?? Array.Empty<Guid>())
        {
            product.ProductCollections.Add(new ProductCollection(product, collectionId));
        }

        var tagIds = (item.TagIds ?? Array.Empty<Guid>())
            .Concat(resolvedTags.GetIds(item.Tags))
            .Distinct();
        foreach (var tagId in tagIds)
        {
            product.ProductTags.Add(new ProductTag(product, tagId));
        }

        product.EnsureHasAtLeastOneVariant();

        return product;
    }

    // Burada toplu ürünlerdeki her varyantı merkezi ad ve değer kayıtlarıyla aynı sırada ilişkilendiriyorum.
    private async Task AssignVariantOptionsAsync(
        IReadOnlyList<Product> products,
        IReadOnlyList<PreparedProductItem> preparedItems,
        CancellationToken cancellationToken)
    {
        for (var productIndex = 0; productIndex < products.Count; productIndex++)
        {
            var variants = products[productIndex].Variants.ToList();
            var requestedVariants = preparedItems[productIndex].Item.Variants ?? [];
            for (var variantIndex = 0; variantIndex < variants.Count; variantIndex++)
            {
                var requestVariant = requestedVariants[variantIndex];
                var resolvedOption = await _variantOptionResolver!.ResolveCompositeAsync(
                    requestVariant.Name,
                    requestVariant.Value,
                    cancellationToken);
                variants[variantIndex].ReplaceOptionValues(resolvedOption);
            }
        }
    }

    // Burada metin listesindeki büyük-küçük harf duyarsız tekrarları yakalıyorum.
    private static void EnsureNoDuplicates(IEnumerable<string> values, string message)
    {
        var duplicates = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new ConflictException($"{message} {string.Join(", ", duplicates)}.");
        }
    }

    // Burada toplu ürün satırının doğrulanmış URL ve ana SKU değerlerini birlikte taşıyorum.
    private sealed record PreparedProductItem(BulkCreateProductItem Item, string Url, string MainSku);
}
