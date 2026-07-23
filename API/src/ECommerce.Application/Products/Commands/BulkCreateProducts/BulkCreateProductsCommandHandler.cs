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
    private readonly ICollectionRepository _collectionRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IProductUrlGenerator _productUrlGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public BulkCreateProductsCommandHandler(
        IProductRepository productRepository,
        IProductTypeRepository productTypeRepository,
        IBrandRepository brandRepository,
        ICollectionRepository collectionRepository,
        ITagRepository tagRepository,
        IProductUrlGenerator productUrlGenerator,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _productTypeRepository = productTypeRepository;
        _brandRepository = brandRepository;
        _collectionRepository = collectionRepository;
        _tagRepository = tagRepository;
        _productUrlGenerator = productUrlGenerator;
        _unitOfWork = unitOfWork;
    }

    // Burada toplu ürün yükleme isteğini kontrol edip ürünleri ilişkileriyle birlikte oluşturuyorum.
    public async Task<IReadOnlyList<ProductDto>> Handle(BulkCreateProductsCommand request, CancellationToken cancellationToken)
    {
        var items = request.Products;
        var preparedItems = items
            .Select(item => new PreparedProductItem(
                item,
                string.IsNullOrWhiteSpace(item.Url) ? _productUrlGenerator.Generate(item.Title) : item.Url.Trim()))
            .ToList();

        EnsureNoDuplicates(preparedItems.Select(item => item.Url), "Product url is duplicated in the request.");

        var existingUrls = await _productRepository.GetExistingUrlsAsync(
            preparedItems.Select(item => item.Url),
            cancellationToken);

        if (existingUrls.Count > 0)
        {
            throw new ConflictException($"Product url already exists: {string.Join(", ", existingUrls)}.");
        }

        await EnsureProductTypesExistAsync(preparedItems, cancellationToken);
        await EnsureBrandsExistAsync(preparedItems, cancellationToken);
        await EnsureCollectionsExistAsync(preparedItems, cancellationToken);
        await EnsureTagsExistAsync(preparedItems, cancellationToken);
        await EnsureVariantSkusAreUniqueAsync(preparedItems, cancellationToken);

        var products = preparedItems
            .Select(item => CreateProduct(item.Item, item.Url))
            .ToList();

        await _productRepository.AddRangeAsync(products, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var productIds = products.Select(product => product.Id).ToList();
        var createdProducts = await _productRepository.GetByIdsAsync(productIds, cancellationToken);
        var createdProductMap = createdProducts.ToDictionary(product => product.Id);

        return productIds
            .Select(id => createdProductMap.TryGetValue(id, out var product) ? product.ToDto() : products.Single(product => product.Id == id).ToDto())
            .ToList();
    }

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

    private static Product CreateProduct(BulkCreateProductItem item, string url)
    {
        var product = new Product(
            item.Title,
            url,
            item.TypeId,
            item.BrandId,
            item.Description,
            item.Status,
            item.IsActive,
            item.IsFeatured,
            item.DisplayOrder,
            item.SeoTitle,
            item.SeoDescription);

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
                variant.IsActive);

            if (variant.Stock > 0)
            {
                productVariant.InventoryTransactions.Add(new InventoryTransaction(
                    productVariant.Id,
                    InventoryTransactionType.StockIn,
                    variant.Stock,
                    variant.Stock,
                    "Initial stock"));
            }

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

        foreach (var tagId in item.TagIds?.Distinct() ?? Array.Empty<Guid>())
        {
            product.ProductTags.Add(new ProductTag(product, tagId));
        }

        product.EnsureHasAtLeastOneVariant();

        return product;
    }

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

    private sealed record PreparedProductItem(BulkCreateProductItem Item, string Url);
}
