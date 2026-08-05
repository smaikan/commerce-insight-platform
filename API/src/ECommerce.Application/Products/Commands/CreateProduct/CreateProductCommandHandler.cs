using ECommerce.Application.Accounting.CostLayers;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductTypeRepository _productTypeRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly ITaxRateRepository _taxRateRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly IProductTagResolver _productTagResolver;
    private readonly IProductTypeNameResolver _productTypeNameResolver;
    private readonly IProductCollectionNameResolver _productCollectionNameResolver;
    private readonly IProductUrlGenerator _productUrlGenerator;

    private readonly IProductUrlResolver _productUrlResolver;
    private readonly IOpeningBalanceCostLayerWriter _openingBalanceCostLayerWriter;
    private readonly IVariantOptionResolver? _variantOptionResolver;
    private readonly IUnitOfWork _unitOfWork;

    // Burada ürün oluşturma akışının ihtiyaç duyduğu bağımlılıkları hazırlıyorum.
    public CreateProductCommandHandler(
        IProductRepository productRepository,
        IProductTypeRepository productTypeRepository,
        IBrandRepository brandRepository,
        ITaxRateRepository taxRateRepository,
        ICollectionRepository collectionRepository,
        IProductTagResolver productTagResolver,
        IProductUrlGenerator productUrlGenerator,

        IOpeningBalanceCostLayerWriter openingBalanceCostLayerWriter,
        IUnitOfWork unitOfWork,
        IVariantOptionResolver? variantOptionResolver = null,
        IProductUrlResolver? productUrlResolver = null,
        IProductTypeNameResolver? productTypeNameResolver = null,
        IProductCollectionNameResolver? productCollectionNameResolver = null)
    {
        _productRepository = productRepository;
        _productTypeRepository = productTypeRepository;
        _brandRepository = brandRepository;
        _taxRateRepository = taxRateRepository;
        _collectionRepository = collectionRepository;
        _productTagResolver = productTagResolver;
        _productUrlGenerator = productUrlGenerator;

        _productUrlResolver = productUrlResolver ?? new ProductUrlResolver(productRepository, productUrlGenerator);
        _productTypeNameResolver = productTypeNameResolver ?? new ProductTypeNameResolver(productTypeRepository);
        _productCollectionNameResolver = productCollectionNameResolver ?? new ProductCollectionNameResolver(
            collectionRepository,
            productUrlGenerator as IUrlGenerator ?? throw new InvalidOperationException(
                "Product URL generator must also implement IUrlGenerator."));
        _openingBalanceCostLayerWriter = openingBalanceCostLayerWriter;
        _variantOptionResolver = variantOptionResolver;
        _unitOfWork = unitOfWork;
    }

    // Burada tek ürün oluşturma isteğini doğrulayıp ürünü kayda hazırlıyorum.
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var normalizedMainSku = request.MainSku.Trim().ToUpperInvariant();
        if (await _productRepository.MainSkuExistsAsync(
                normalizedMainSku,
                cancellationToken: cancellationToken))
        {
            throw new ConflictException("Product main SKU already exists.");
        }

        var typeId = await _productTypeNameResolver.ResolveAsync(request.Type, cancellationToken);

        if (request.BrandId.HasValue && !await _brandRepository.ExistsAsync(request.BrandId.Value, cancellationToken))
        {
            throw new NotFoundException("Brand was not found.");
        }

        var taxRate = await ResolveActiveTaxRateAsync(request.TaxRateId, cancellationToken);

        var collectionIds = await _productCollectionNameResolver.ResolveAsync(request.Collections, cancellationToken);

        var resolvedTags = request.Tags is { Count: > 0 }
            ? await _productTagResolver.ResolveAsync(request.Tags, cancellationToken)
            : ProductTagResolution.Empty;

        var variants = request.Variants ?? [];
        var normalizedSkus = variants.Select(variant => variant.Sku.Trim()).ToList();
        var duplicateSkus = normalizedSkus
            .GroupBy(sku => sku, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateSkus.Count > 0)
        {
            throw new ConflictException($"Variant SKU is duplicated in the request: {string.Join(", ", duplicateSkus)}.");
        }

        var existingSkus = await _productRepository.GetExistingVariantSkusAsync(normalizedSkus, cancellationToken);
        if (existingSkus.Count > 0)
        {
            throw new ConflictException($"Variant SKU already exists: {string.Join(", ", existingSkus)}.");
        }

        var url = await _productUrlResolver.ResolveAsync(
            request.Title,
            request.Url,
            cancellationToken: cancellationToken);

        var product = new Product(
            request.Title,
            url,
            normalizedMainSku,
            typeId,
            request.BrandId,
            request.Description,
            request.Status,
            request.IsActive,
            request.IsFeatured,
            request.DisplayOrder,
            request.SeoTitle,
            request.SeoDescription,
            request.TaxRateId,
            request.HasVariants);

        foreach (var collectionId in collectionIds)
        {
            product.ProductCollections.Add(new ProductCollection(product, collectionId));
        }

        foreach (var tagId in resolvedTags.GetIds(request.Tags))
        {
            product.ProductTags.Add(new ProductTag(product, tagId));
        }

        var openingBalanceSeeds =
            new List<OpeningBalanceCostLayerSeed>(variants.Count);
        foreach (var item in variants)
        {
            var resolvedOption = _variantOptionResolver is null
                ? null
                : await _variantOptionResolver.ResolveCompositeAsync(item.Name, item.Value, cancellationToken);
            var variant = new ProductVariant(
                product,
                item.Name,
                item.Sku,
                item.Price,
                item.Stock,
                item.CompareAtPrice,
                item.Barcode,
                item.Material,
                item.IsActive,
                taxRate?.CalculateNetPrice(item.Price) ?? item.Price,
                item.Value);
            if (resolvedOption is not null)
            {
                variant.ReplaceOptionValues(resolvedOption);
            }

            product.Variants.Add(variant);
            openingBalanceSeeds.Add(new OpeningBalanceCostLayerSeed(
                variant,
                item.OpeningUnitCostExcludingVat,
                item.OpeningUnitCostIncludingVat));
        }

        product.EnsureHasAtLeastOneVariant();

        await _productRepository.AddAsync(product, cancellationToken);
        await _openingBalanceCostLayerWriter.CreateForNewVariantsAsync(
            openingBalanceSeeds,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdProduct = await _productRepository.GetByIdAsync(product.Id, cancellationToken);
        return createdProduct?.ToDto() ?? product.ToDto();
    }

    // Burada seçilen vergi oranını aktif kayıttan çözerek fiyat hesaplamasına hazırlıyorum.
    private async Task<TaxRate?> ResolveActiveTaxRateAsync(
        Guid? taxRateId,
        CancellationToken cancellationToken)
    {
        if (!taxRateId.HasValue)
        {
            return null;
        }

        var taxRate = await _taxRateRepository.GetByIdAsync(taxRateId.Value, cancellationToken);
        if (taxRate is null || !taxRate.IsActive)
        {
            throw new NotFoundException("Tax rate was not found or is inactive.");
        }

        return taxRate;
    }
}
