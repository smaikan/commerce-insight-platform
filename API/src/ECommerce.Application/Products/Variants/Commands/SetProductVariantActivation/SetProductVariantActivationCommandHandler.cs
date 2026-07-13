using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Variants.Commands.SetProductVariantActivation;

public sealed class SetProductVariantActivationCommandHandler : IRequestHandler<SetProductVariantActivationCommand, ProductVariantDto>
{
    private readonly IProductVariantRepository _variantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetProductVariantActivationCommandHandler(IProductVariantRepository variantRepository, IUnitOfWork unitOfWork)
    {
        _variantRepository = variantRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada varyantın satışa açık olup olmayacağını domain metotlarıyla değiştiriyorum.
    public async Task<ProductVariantDto> Handle(SetProductVariantActivationCommand request, CancellationToken cancellationToken)
    {
        var variant = await _variantRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (variant is null)
        {
            throw new NotFoundException("Product variant was not found.");
        }

        if (request.IsActive)
        {
            variant.Activate();
        }
        else
        {
            variant.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return variant.ToDto();
    }
}
