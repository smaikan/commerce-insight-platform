using ECommerce.Application.Accounting.CostLayers;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Products.Variants.Commands.CreateProductVariant;

public sealed class CreateProductVariantCommandHandler : IRequestHandler<CreateProductVariantCommand, ProductVariantDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductVariantRepository _variantRepository;
    private readonly IOpeningBalanceCostLayerWriter _openingBalanceCostLayerWriter;
    private readonly IVariantOptionResolver? _variantOptionResolver;
    private readonly IUnitOfWork _unitOfWork;

    // Burada varyant oluşturma akışının ürün, varyant ve transaction bağımlılıklarını hazırlıyorum.
    public CreateProductVariantCommandHandler(
        IProductRepository productRepository,
        IProductVariantRepository variantRepository,
        IOpeningBalanceCostLayerWriter openingBalanceCostLayerWriter,
        IUnitOfWork unitOfWork,
        IVariantOptionResolver? variantOptionResolver = null)
    {
        _productRepository = productRepository;
        _variantRepository = variantRepository;
        _openingBalanceCostLayerWriter = openingBalanceCostLayerWriter;
        _variantOptionResolver = variantOptionResolver;
        _unitOfWork = unitOfWork;
    }

    // Burada ürüne yeni satılabilir varyant eklemeden önce ürün ve SKU bilgisini kontrol ediyorum.
    public async Task<ProductVariantDto> Handle(CreateProductVariantCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product was not found.");
        }

        if (await _variantRepository.SkuExistsAsync(request.Sku, cancellationToken: cancellationToken))
        {
            throw new ConflictException("Product variant SKU already exists.");
        }

        var resolvedOption = _variantOptionResolver is null
            ? null
            : await _variantOptionResolver.ResolveCompositeAsync(request.Name, request.Value, cancellationToken);

        var variant = new ProductVariant(
            request.ProductId,
            request.Name,
            request.Sku,
            request.Price,
            request.Stock,
            request.CompareAtPrice,
            request.Barcode,
            request.Material,
            request.IsActive,
            product.TaxRate?.CalculateNetPrice(request.Price) ?? request.Price,
            request.Value);
        if (resolvedOption is not null)
        {
            variant.ReplaceOptionValues(resolvedOption);
        }

        await _variantRepository.AddAsync(variant, cancellationToken);
        await _openingBalanceCostLayerWriter.CreateForNewVariantsAsync(
            [new OpeningBalanceCostLayerSeed(
                variant,
                request.OpeningUnitCostExcludingVat,
                request.OpeningUnitCostIncludingVat)],
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return variant.ToDto();
    }
}
