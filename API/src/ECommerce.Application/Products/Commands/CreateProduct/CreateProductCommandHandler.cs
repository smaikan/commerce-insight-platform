using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductTypeRepository _productTypeRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly IProductUrlGenerator _productUrlGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(
        IProductRepository productRepository,
        IProductTypeRepository productTypeRepository,
        IBrandRepository brandRepository,
        IProductUrlGenerator productUrlGenerator,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _productTypeRepository = productTypeRepository;
        _brandRepository = brandRepository;
        _productUrlGenerator = productUrlGenerator;
        _unitOfWork = unitOfWork;
    }

    // Burada tek ürün oluşturma isteğini doğrulayıp ürünü kayda hazırlıyorum.
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (!await _productTypeRepository.ExistsAsync(request.TypeId, cancellationToken))
        {
            throw new NotFoundException("Product type was not found.");
        }

        if (request.BrandId.HasValue && !await _brandRepository.ExistsAsync(request.BrandId.Value, cancellationToken))
        {
            throw new NotFoundException("Brand was not found.");
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

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdProduct = await _productRepository.GetByIdAsync(product.Id, cancellationToken);
        return createdProduct?.ToDto() ?? product.ToDto();
    }
}
