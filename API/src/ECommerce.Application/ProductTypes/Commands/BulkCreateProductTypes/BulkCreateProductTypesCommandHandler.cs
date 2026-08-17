using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.ProductTypes.Dtos;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.ProductTypes.Commands.BulkCreateProductTypes;

public sealed class BulkCreateProductTypesCommandHandler
    : IRequestHandler<BulkCreateProductTypesCommand, IReadOnlyList<ProductTypeDto>>
{
    private readonly IProductTypeRepository _productTypeRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada toplu ürün türü yazma bağımlılıklarını hazırlıyorum.
    public BulkCreateProductTypesCommandHandler(
        IProductTypeRepository productTypeRepository,
        IUnitOfWork unitOfWork)
    {
        _productTypeRepository = productTypeRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada ürün tiplerini toplu şekilde oluşturmadan önce isim çakışmalarını kontrol ediyorum.
    public async Task<IReadOnlyList<ProductTypeDto>> Handle(
        BulkCreateProductTypesCommand request,
        CancellationToken cancellationToken)
    {
        EnsureNoDuplicates(request.ProductTypes.Select(item => item.Name));

        var existingNames = await _productTypeRepository.GetExistingNamesAsync(
            request.ProductTypes.Select(item => item.Name),
            cancellationToken);

        if (existingNames.Count > 0)
        {
            throw new ConflictException($"Product type name already exists: {string.Join(", ", existingNames)}.");
        }

        var productTypes = request.ProductTypes
            .Select(item => new ProductType(
                item.Name,
                item.Description,
                item.IsActive,
                item.ImageUrl))
            .ToList();

        await _productTypeRepository.AddRangeAsync(productTypes, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return productTypes.Select(productType => productType.ToDto()).ToList();
    }

    // Burada aynı toplu istekte yinelenen ürün türü adlarını engelliyorum.
    private static void EnsureNoDuplicates(IEnumerable<string> names)
    {
        var duplicates = names
            .GroupBy(name => name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new ConflictException($"Product type name is duplicated in the request: {string.Join(", ", duplicates)}.");
        }
    }
}
