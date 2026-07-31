using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.ShippingMethods.Dtos;
using MediatR;

namespace ECommerce.Application.ShippingMethods.Commands.UpdateShippingMethod;

public sealed class UpdateShippingMethodCommandHandler : IRequestHandler<UpdateShippingMethodCommand, ShippingMethodDto>
{
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada kargo yöntemi güncelleme use-case'i için gerekli kalıcılık bağımlılıklarını hazırlıyorum.
    public UpdateShippingMethodCommandHandler(
        IShippingMethodRepository shippingMethodRepository,
        IUnitOfWork unitOfWork)
    {
        _shippingMethodRepository = shippingMethodRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada hedef kaydı ve ad çakışmasını denetleyip kargo yöntemini güncelliyorum.
    public async Task<ShippingMethodDto> Handle(UpdateShippingMethodCommand request, CancellationToken cancellationToken)
    {
        var shippingMethod = await _shippingMethodRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (shippingMethod is null)
        {
            throw new NotFoundException("Shipping method was not found.");
        }

        if (await _shippingMethodRepository.NameExistsAsync(request.Name, request.Id, cancellationToken))
        {
            throw new ConflictException("Shipping method name already exists.");
        }

        shippingMethod.Rename(request.Name);
        shippingMethod.ChangeFixedFee(request.FixedFee);
        shippingMethod.ChangeDisplayOrder(request.DisplayOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return shippingMethod.ToDto();
    }
}
