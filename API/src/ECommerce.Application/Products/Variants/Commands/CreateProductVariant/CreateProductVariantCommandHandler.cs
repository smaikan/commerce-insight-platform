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
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductVariantCommandHandler(
        IProductRepository productRepository,
        IProductVariantRepository variantRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _variantRepository = variantRepository;
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

        var variant = new ProductVariant(
            request.ProductId,
            request.Sku,
            request.Price,
            request.Stock,
            request.CompareAtPrice,
            request.Barcode,
            request.Color,
            request.Size,
            request.Material,
            request.IsActive);

        await _variantRepository.AddAsync(variant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return variant.ToDto();
    }
}
