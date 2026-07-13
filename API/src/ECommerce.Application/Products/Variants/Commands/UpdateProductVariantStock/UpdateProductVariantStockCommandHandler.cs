using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Variants.Commands.UpdateProductVariantStock;

public sealed class UpdateProductVariantStockCommandHandler : IRequestHandler<UpdateProductVariantStockCommand, ProductVariantDto>
{
    private readonly IProductVariantRepository _variantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductVariantStockCommandHandler(IProductVariantRepository variantRepository, IUnitOfWork unitOfWork)
    {
        _variantRepository = variantRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada stok bilgisini Product yerine doğrudan varyant üzerinde güncelliyorum.
    public async Task<ProductVariantDto> Handle(UpdateProductVariantStockCommand request, CancellationToken cancellationToken)
    {
        var variant = await _variantRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (variant is null)
        {
            throw new NotFoundException("Product variant was not found.");
        }

        variant.UpdateStock(request.Stock);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return variant.ToDto();
    }
}
