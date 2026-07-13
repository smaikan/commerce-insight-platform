using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductTypeRepository _productTypeRepository;
    private readonly IBrandRepository _brandRepository;
    private readonly IProductUrlGenerator _productUrlGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(
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

    // Burada ürünü güncellemeden önce bağlı kayıtları ve URL çakışmasını kontrol ediyorum.
    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product was not found.");
        }

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

        if (await _productRepository.UrlExistsAsync(url, request.Id, cancellationToken))
        {
            throw new ConflictException("Product url already exists.");
        }

        product.UpdateBasics(
            request.Title,
            url,
            request.Description,
            request.DisplayOrder,
            request.SeoTitle,
            request.SeoDescription);

        product.ChangeType(request.TypeId);
        product.ChangeBrand(request.BrandId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedProduct = await _productRepository.GetByIdAsync(product.Id, cancellationToken);
        return updatedProduct?.ToDto() ?? product.ToDto();
    }
}
