using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductTypeRepository _productTypeRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly ITaxRateRepository _taxRateRepository;
    private readonly IProductTagResolver _productTagResolver;
    private readonly IProductUrlGenerator _productUrlGenerator;
    private readonly IProductUrlResolver _productUrlResolver;
    private readonly IUnitOfWork _unitOfWork;

    // Burada ürün güncelleme akışının ihtiyaç duyduğu bağımlılıkları hazırlıyorum.
    public UpdateProductCommandHandler(
        IProductRepository productRepository,
        IProductTypeRepository productTypeRepository,
        IBrandRepository brandRepository,
        ITaxRateRepository taxRateRepository,
        IProductTagResolver productTagResolver,
        IProductUrlGenerator productUrlGenerator,
        IUnitOfWork unitOfWork,
        IProductUrlResolver? productUrlResolver = null)
    {
        _productRepository = productRepository;
        _productTypeRepository = productTypeRepository;
        _brandRepository = brandRepository;
        _taxRateRepository = taxRateRepository;
        _productTagResolver = productTagResolver;
        _productUrlGenerator = productUrlGenerator;
        _productUrlResolver = productUrlResolver ?? new ProductUrlResolver(productRepository, productUrlGenerator);
        _unitOfWork = unitOfWork;
    }

    // Burada ürünü güncellemeden önce bağlı kayıt, ana SKU ve URL çakışmalarını kontrol ediyorum.
    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = request.Tags is null
            ? await _productRepository.GetByIdForUpdateAsync(request.Id, cancellationToken)
            : await _productRepository.GetWithRelationsForUpdateAsync(request.Id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product was not found.");
        }

        var normalizedMainSku = request.MainSku.Trim().ToUpperInvariant();
        if (await _productRepository.MainSkuExistsAsync(
                normalizedMainSku,
                request.Id,
                cancellationToken))
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

        var url = await _productUrlResolver.ResolveAsync(
            request.Title,
            request.Url,
            request.Id,
            cancellationToken: cancellationToken);

        if (!string.Equals(product.Url, url, StringComparison.OrdinalIgnoreCase))
        {
            await _productRepository.AddUrlRedirectAsync(
                new ProductUrlRedirect(product, product.Url),
                cancellationToken);
        }

        product.UpdateBasics(
            request.Title,
            url,
            request.Description,
            request.DisplayOrder,
            request.SeoTitle,
            request.SeoDescription,
            normalizedMainSku);

        product.ChangeType(request.TypeId);
        product.ChangeBrand(request.BrandId);
        product.ChangeTaxRate(request.TaxRateId);
        foreach (var variant in product.Variants)
        {
            variant.RecalculateNetPrice(taxRate);
        }

        if (request.Tags is not null)
        {
            var resolvedTags = request.Tags.Count == 0
                ? ProductTagResolution.Empty
                : await _productTagResolver.ResolveAsync(request.Tags, cancellationToken);
            var tagIds = resolvedTags.GetIds(request.Tags).ToHashSet();
            var removedProductTags = product.ProductTags
                .Where(productTag => !tagIds.Contains(productTag.TagId))
                .ToList();
            foreach (var productTag in removedProductTags)
            {
                product.ProductTags.Remove(productTag);
            }

            var currentTagIds = product.ProductTags
                .Select(productTag => productTag.TagId)
                .ToHashSet();
            foreach (var tagId in tagIds.Where(tagId => !currentTagIds.Contains(tagId)))
            {
                product.ProductTags.Add(new ProductTag(product.Id, tagId));
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedProduct = await _productRepository.GetByIdAsync(product.Id, cancellationToken);
        return updatedProduct?.ToDto() ?? product.ToDto();
    }

    // Burada ürün fiyatlarının hesaplanması için yalnız etkin vergi oranını çözümlüyorum.
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
