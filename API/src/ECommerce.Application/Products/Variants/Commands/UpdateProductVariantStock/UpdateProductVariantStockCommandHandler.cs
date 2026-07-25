using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Products.Variants.Commands.UpdateProductVariantStock;

public sealed class UpdateProductVariantStockCommandHandler : IRequestHandler<UpdateProductVariantStockCommand, ProductVariantDto>
{
    private readonly IProductVariantRepository _variantRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada stok güncelleme bağımlılıklarını hazırlıyorum.
    public UpdateProductVariantStockCommandHandler(IProductVariantRepository variantRepository, IUnitOfWork unitOfWork)
    {
        _variantRepository = variantRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada imzalı stok hareketini varyantın tek ledger girişinden uygulayıp bakiyeyi güncelliyorum.
    public async Task<ProductVariantDto> Handle(UpdateProductVariantStockCommand request, CancellationToken cancellationToken)
    {
        var variant = await _variantRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (variant is null)
        {
            throw new NotFoundException("Product variant was not found.");
        }

        variant.ApplyStockMovement(request.QuantityDelta, request.Type, request.Reason);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return variant.ToDto();
    }
}
