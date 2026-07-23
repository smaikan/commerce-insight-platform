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
    private readonly ICollectionRepository _collectionRepository;
    private readonly IProductUrlGenerator _productUrlGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(
        IProductRepository productRepository,
        IProductTypeRepository productTypeRepository,
        IBrandRepository brandRepository,
        ICollectionRepository collectionRepository,
        IProductUrlGenerator productUrlGenerator,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _productTypeRepository = productTypeRepository;
        _brandRepository = brandRepository;
        _collectionRepository = collectionRepository;
        _productUrlGenerator = productUrlGenerator;
        _unitOfWork = unitOfWork;
    }

    // Burada tek ürün oluşturma isteğini doğrulayıp ürünü kayda hazırlıyorum.
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (request.TypeId.HasValue &&
            !await _productTypeRepository.ExistsAsync(request.TypeId.Value, cancellationToken))
        {
            throw new NotFoundException("Product type was not found.");
        }

        if (request.BrandId.HasValue && !await _brandRepository.ExistsAsync(request.BrandId.Value, cancellationToken))
        {
            throw new NotFoundException("Brand was not found.");
        }

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
            request.TypeId,
            request.BrandId,
            request.Description,
            request.Status,
            request.IsActive,
            request.IsFeatured,
            request.DisplayOrder,
            request.SeoTitle,
            request.SeoDescription);

        foreach (var collectionId in collectionIds)
        {
            product.ProductCollections.Add(new ProductCollection(product, collectionId));
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
                item.IsActive);

            if (item.Stock > 0)
            {
                variant.InventoryTransactions.Add(new InventoryTransaction(
                    variant.Id,
                    InventoryTransactionType.StockIn,
                    item.Stock,
                    item.Stock,
                    "Initial stock"));
            }

            product.Variants.Add(variant);
        }

        product.EnsureHasAtLeastOneVariant();

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdProduct = await _productRepository.GetByIdAsync(product.Id, cancellationToken);
        return createdProduct?.ToDto() ?? product.ToDto();
    }
}
