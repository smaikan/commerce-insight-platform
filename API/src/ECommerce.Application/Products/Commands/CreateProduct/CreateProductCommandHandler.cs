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
    private readonly IProductUrlGenerator _productUrlGenerator;
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
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _productTypeRepository = productTypeRepository;
        _brandRepository = brandRepository;
        _taxRateRepository = taxRateRepository;
        _collectionRepository = collectionRepository;
        _productTagResolver = productTagResolver;
        _productUrlGenerator = productUrlGenerator;
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

        if (request.TypeId.HasValue &&
            !await _productTypeRepository.ExistsAsync(request.TypeId.Value, cancellationToken))
        {
            throw new NotFoundException("Product type was not found.");
        }

        if (request.BrandId.HasValue && !await _brandRepository.ExistsAsync(request.BrandId.Value, cancellationToken))
        {
            throw new NotFoundException("Brand was not found.");
        }

        var taxRate = await ResolveActiveTaxRateAsync(request.TaxRateId, cancellationToken);

        var collectionIds = request.CollectionIds?.Distinct().ToList() ?? [];
        if (collectionIds.Count > 0)
        {
            var existingCollectionIds = await _collectionRepository.GetExistingIdsAsync(collectionIds, cancellationToken);
            var missingCollectionIds = collectionIds.Where(id => !existingCollectionIds.Contains(id)).ToList();

            if (missingCollectionIds.Count > 0)
            {
                throw new NotFoundException($"Collection was not found: {string.Join(", ", missingCollectionIds)}.");
            }
        }

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

        var url = string.IsNullOrWhiteSpace(request.Url)
            ? _productUrlGenerator.Generate(request.Title)
            : request.Url.Trim();

        if (await _productRepository.UrlExistsAsync(url, cancellationToken: cancellationToken))
        {
            throw new ConflictException("Product url already exists.");
        }

        var product = new Product(
            request.Title,
            url,
            normalizedMainSku,
            request.TypeId,
            request.BrandId,
            request.Description,
            request.Status,
            request.IsActive,
            request.IsFeatured,
            request.DisplayOrder,
            request.SeoTitle,
            request.SeoDescription,
            request.TaxRateId);

        foreach (var collectionId in collectionIds)
        {
            product.ProductCollections.Add(new ProductCollection(product, collectionId));
        }

        foreach (var tagId in resolvedTags.GetIds(request.Tags))
        {
            product.ProductTags.Add(new ProductTag(product, tagId));
        }

        foreach (var item in variants)
        {
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
                taxRate?.CalculateNetPrice(item.Price) ?? item.Price);

            product.Variants.Add(variant);
        }

        product.EnsureHasAtLeastOneVariant();

        await _productRepository.AddAsync(product, cancellationToken);
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
